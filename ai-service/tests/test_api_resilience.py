"""HTTP contracts and bounded failure paths using deterministic dependencies."""
import time
import threading
from concurrent.futures import ThreadPoolExecutor

import pytest
from fastapi.testclient import TestClient

import main
import app.api.ocr_routes as results_route
from app.api.deps import get_detector, get_geocoder, get_ocr_engine
from app.core.config import get_settings
from app.core.errors import GeocodingUnavailableError, ModelUnavailableError
from test_image_processing import image_bytes


class EmptyOcr:
    def read(self, image):
        return []


class EmptyDetector:
    def detect(self, image):
        return []


class EmptyProvider:
    name = 'test'
    attribution = 'test fixture'

    def search(self, query, limit):
        return []


@pytest.fixture
def client(monkeypatch, tmp_path):
    settings = get_settings()
    monkeypatch.setattr(settings, 'preload_models', False)
    monkeypatch.setattr(settings, 'geocoder_enabled', True)
    monkeypatch.setattr(settings, 'data_dir', tmp_path)
    monkeypatch.setattr(settings, 'max_image_mb', 1)
    old = dict(main.app.dependency_overrides)
    main.app.dependency_overrides.update({
        get_ocr_engine: EmptyOcr, get_detector: EmptyDetector,
        get_geocoder: EmptyProvider,
    })
    monkeypatch.setattr(results_route, 'get_detector', EmptyDetector)
    monkeypatch.setattr(results_route, 'get_ocr_engine', EmptyOcr)
    try:
        with TestClient(main.app, raise_server_exceptions=False) as http:
            yield http
    finally:
        main.app.dependency_overrides.clear()
        main.app.dependency_overrides.update(old)


def check_error(response, status, code):
    assert response.status_code == status, response.text
    body = response.json()
    assert body['success'] is False and body['error']['code'] == code
    assert body['request_id'] == response.headers['x-request-id']
    assert body['processing_ms'] >= 0
    assert 'Traceback' not in response.text


UPLOADS = ['/api/v1/ocr/license-plate', '/api/v1/ocr/document',
           '/api/v1/image/analyze', '/api/v1/location/from-document', '/v1/results']


@pytest.mark.parametrize('route', UPLOADS)
@pytest.mark.parametrize(('payload', 'status', 'code'), [
    (b'fake PNG', 400, 'INVALID_IMAGE'),
    (b'x' * (1024 * 1024 + 1), 413, 'IMAGE_TOO_LARGE'),
])
def test_invalid_uploads(client, route, payload, status, code):
    check_error(client.post(route, files={'file': ('fake.png', payload, 'image/png')}), status, code)


@pytest.mark.parametrize('route', UPLOADS)
def test_missing_upload(client, route):
    response = client.post(route)
    check_error(response, 422 if route == '/v1/results' else 400,
                'VALIDATION_ERROR' if route == '/v1/results' else 'MISSING_IMAGE')


def test_location_success_and_validation(client):
    assert client.get('/health').status_code == 200
    response = client.post('/api/v1/location/search', json={'text': 'Ha Noi'})
    assert response.status_code == 200
    data = response.json()['data']
    assert data['found'] is False and data['verified'] is False
    check_error(client.post('/api/v1/location/search', json={'text': '', 'limit': 0}),
                422, 'VALIDATION_ERROR')


def test_provider_outage(client):
    class BrokenProvider(EmptyProvider):
        def search(self, query, limit):
            raise GeocodingUnavailableError('Unavailable')
    main.app.dependency_overrides[get_geocoder] = BrokenProvider
    response = client.post('/api/v1/location/search', json={'text': 'Ha Noi'})
    assert response.status_code == 503
    assert response.json()['success'] is False


def test_model_unavailable(client):
    class BrokenDetector:
        def detect(self, image):
            raise ModelUnavailableError('Unavailable')
    main.app.dependency_overrides[get_detector] = BrokenDetector
    check_error(client.post('/api/v1/image/analyze', files={'file': ('a.png', image_bytes())}),
                503, 'MODEL_UNAVAILABLE')


def test_timeout_and_recovery(client, monkeypatch):
    class SlowDetector:
        def detect(self, image):
            time.sleep(0.15)
            return []
    monkeypatch.setattr(get_settings(), 'request_timeout_s', 0.03)
    main.app.dependency_overrides[get_detector] = SlowDetector
    start = time.perf_counter()
    response = client.post('/api/v1/image/analyze', files={'file': ('a.png', image_bytes())})
    check_error(response, 504, 'PROCESSING_TIMEOUT')
    assert time.perf_counter() - start < 2
    main.app.dependency_overrides[get_detector] = EmptyDetector
    monkeypatch.setattr(get_settings(), 'request_timeout_s', 1)
    assert client.post('/api/v1/image/analyze', files={'file': ('a.png', image_bytes())}).status_code == 200


def test_result_not_found_and_review_validation(client):
    assert client.get('/v1/results/AI-REQ-20990101-ABCDEF12').status_code == 404
    result = client.post('/v1/results', files={'file': ('a.png', image_bytes())}).json()
    response = client.post('/v1/results/' + result['requestId'] + '/review',
                           json={'decision': 'reject', 'note': ''})
    assert response.status_code in (400, 422)


def test_concurrent_requests_respect_worker_limit(client, monkeypatch):
    class CountingDetector:
        def __init__(self):
            self.lock = threading.Lock()
            self.active = 0
            self.peak = 0

        def detect(self, image):
            with self.lock:
                self.active += 1
                self.peak = max(self.peak, self.active)
            try:
                time.sleep(0.04)
                return []
            finally:
                with self.lock:
                    self.active -= 1

    detector = CountingDetector()
    main.app.dependency_overrides[get_detector] = lambda: detector
    monkeypatch.setattr(get_settings(), 'request_timeout_s', 2)
    limit = get_settings().max_concurrent_jobs
    payload = image_bytes()

    def send(_):
        return client.post('/api/v1/image/analyze', files={'file': ('a.png', payload)})

    with ThreadPoolExecutor(max_workers=4) as pool:
        responses = list(pool.map(send, range(4)))
    assert all(response.status_code == 200 for response in responses)
    assert 1 <= detector.peak <= limit
    assert len({r.headers['x-request-id'] for r in responses}) == 4
