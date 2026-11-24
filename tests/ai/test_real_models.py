"""Opt-in image-to-JSON tests. Run with AI_RUN_REAL_MODELS=1."""
import importlib.util
import os
import sys
import time
from io import BytesIO
from pathlib import Path

import pytest

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'ai-service'))

pytestmark = [pytest.mark.real_model, pytest.mark.skipif(
    os.getenv('AI_RUN_REAL_MODELS') != '1', reason='Set AI_RUN_REAL_MODELS=1 to run installed models')]


@pytest.fixture(scope='module')
def client(tmp_path_factory):
    from fastapi.testclient import TestClient
    import main
    from app.core.config import get_settings
    settings = get_settings()
    with pytest.MonkeyPatch.context() as patch:
        patch.setattr(settings, 'preload_models', False)
        patch.setattr(settings, 'geocoder_enabled', False)
        patch.setattr(settings, 'ocr_engine', 'rapidocr')
        patch.setattr(settings, 'request_timeout_s', 120)
        patch.setattr(settings, 'data_dir', tmp_path_factory.mktemp('ai-results'))
        from app.api.deps import get_ocr_engine, get_detector
        get_ocr_engine.cache_clear()
        get_detector.cache_clear()
        try:
            with TestClient(main.app) as http:
                yield http
        finally:
            get_ocr_engine.cache_clear()
            get_detector.cache_clear()


def upload(client, route, data, record_property):
    start = time.perf_counter()
    response = client.post(route, files={'file': ('fixture.png', data, 'image/png')})
    elapsed = time.perf_counter() - start
    record_property('latency_seconds', round(elapsed, 3))
    record_property('request_id', response.headers.get('x-request-id', ''))
    assert response.status_code == 200, response.text
    # CPU smoke budget, deliberately distinct from production's default 20 s.
    assert elapsed < float(os.getenv('AI_TEST_MAX_SECONDS', '120'))
    return response.json()


def picture(lines, blur=False):
    from PIL import Image, ImageDraw, ImageFont, ImageFilter
    image = Image.new('RGB', (1100, 600), 'white')
    font = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 56)
    draw = ImageDraw.Draw(image)
    for i, text in enumerate(lines):
        draw.text((40, 30 + i * 110), text, font=font, fill='black')
    if blur:
        image = image.filter(ImageFilter.GaussianBlur(1.2))
    stream = BytesIO()
    image.save(stream, 'PNG')
    return stream.getvalue()


@pytest.mark.parametrize(('lines', 'expected', 'blur'), [
    (['29A-123.45'], {'29A12345'}, False),
    (['29A-123.45'], {'29A12345'}, True),
    (['29A-123.45', '51B-678.90'], {'29A12345', '51B67890'}, False),
    (['29A', '123.45'], {'29A12345'}, False),
])
def test_plate_accuracy(client, lines, expected, blur, record_property):
    result = upload(client, '/api/v1/ocr/license-plate', picture(lines, blur), record_property)['data']
    actual = {''.join(c for c in x['plate'] if c.isalnum())
              for x in result['candidates'] if x['plate']}
    record_property('expected', ','.join(sorted(expected)))
    record_property('actual', ','.join(sorted(actual)))
    assert expected <= actual


def test_document_image_accuracy(client, record_property):
    image = picture(['CAN CUOC CONG DAN', 'So: 001099012345', 'Ho va ten: TEST'])
    result = upload(client, '/api/v1/ocr/document', image, record_property)['data']
    assert result['document_type'] == 'cccd'
    assert result['id_number']['value'] == '001099012345'
    assert result['full_name']['value'] == 'TEST'


def test_official_blank_identity_template(client, record_property):
    """Public 2024 card illustration has labels but no personal field values."""
    image = Path(__file__).parent / 'samples' / 'public-id-template.webp'
    result = upload(client, '/api/v1/ocr/document', image.read_bytes(), record_property)['data']
    record_property('source', 'https://bocongan.gov.vn/bai-viet/ban-hanh-mau-the-can-cuoc-mau-giay-chung-nhan-can-cuoc-su-dung-tu-0172024-d1-t1415')
    assert result['is_valid_document'] is False
    assert result['id_number'] is None
    assert result['full_name'] is None


@pytest.mark.parametrize('route', ['/api/v1/ocr/document', '/api/v1/ocr/license-plate', '/api/v1/image/analyze'])
def test_blank_image(client, route, record_property):
    data = upload(client, route, picture([]), record_property)['data']
    if route.endswith('document'):
        assert data['is_valid_document'] is False and data['id_number'] is None
    elif route.endswith('license-plate'):
        assert data['found'] is False
    else:
        assert data['objects'] == []


def test_yolo_bus_and_people(client, record_property):
    spec = importlib.util.find_spec('ultralytics')
    assert spec and spec.origin, 'Install ultralytics from ai-service/requirements.txt'
    asset = Path(spec.origin).parent / 'assets' / 'bus.jpg'
    assert asset.is_file(), 'Install Ultralytics sample bus.jpg or provide the documented fixture'
    data = upload(client, '/api/v1/image/analyze', asset.read_bytes(), record_property)['data']
    assert {'bus', 'person'} <= {item['label'] for item in data['objects']}
    for item in data['objects']:
        b = item['box']
        assert 0 <= b['xMin'] < b['xMax'] <= data['image']['width']
        assert 0 <= b['yMin'] < b['yMax'] <= data['image']['height']


def test_combined_result_roundtrip(client, record_property):
    result = upload(client, '/v1/results', picture(['TEST STREET']), record_property)
    assert result['status'] == 'completed'
    assert any('TEST' in item['text'] for item in result['ocr'])
    stored = client.get('/v1/results/' + result['requestId'])
    assert stored.status_code == 200
    assert stored.json()['requestId'] == result['requestId']
    assert client.get(result['image']['url']).status_code == 200
