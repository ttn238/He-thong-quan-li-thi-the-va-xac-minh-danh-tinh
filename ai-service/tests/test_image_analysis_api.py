from contextlib import asynccontextmanager
from io import BytesIO
from pathlib import Path
from uuid import uuid4

import httpx
import numpy as np
import pytest
from PIL import Image

import main
import app.api.ocr_routes as result_routes
from app.core.config import get_settings
from app.services.object_detection import RawDetection
from app.services.ocr_service import OcrLine


class StubDetector:
    def detect(self, _image):
        return [RawDetection(class_id=0, confidence=0.93, box=(12, 16, 58, 80))]


class StubOcrEngine:
    def read(self, _image):
        return [OcrLine(text="TEST STREET", confidence=0.91, box=(8, 10, 90, 25))]


@pytest.fixture
def app(monkeypatch):
    settings = get_settings()
    test_data_root = Path(__file__).parent / ".test-data"
    test_data_root.mkdir(exist_ok=True)
    data_dir = test_data_root / uuid4().hex
    data_dir.mkdir()
    monkeypatch.setattr(settings, "data_dir", data_dir)
    monkeypatch.setattr(settings, "preload_models", False)
    monkeypatch.setattr(result_routes, "get_detector", StubDetector)
    monkeypatch.setattr(result_routes, "get_ocr_engine", StubOcrEngine)
    try:
        yield main.app
    finally:
        for path in data_dir.iterdir():
            if path.is_file():
                path.unlink()
        data_dir.rmdir()
        if not any(test_data_root.iterdir()):
            test_data_root.rmdir()


@pytest.fixture
def anyio_backend():
    return "asyncio"


@asynccontextmanager
async def asgi_client(app):
    async with app.router.lifespan_context(app):
        transport = httpx.ASGITransport(app=app)
        async with httpx.AsyncClient(transport=transport, base_url="http://test") as client:
            yield client


def png_bytes() -> bytes:
    image = Image.fromarray(np.full((128, 128, 3), 160, dtype=np.uint8))
    output = BytesIO()
    image.save(output, format="PNG")
    return output.getvalue()


async def upload(client: httpx.AsyncClient):
    return await client.post(
        "/v1/results",
        files={"file": ("sample.png", png_bytes(), "image/png")},
        data={"linked_record_id": "TB-TEST"},
    )


@pytest.mark.anyio
async def test_analyze_returns_shared_camel_case_contract(app):
    async with asgi_client(app) as client:
        response = await upload(client)

    assert response.status_code == 200
    result = response.json()
    assert result["status"] == "completed"
    assert result["linkedRecordId"] == "TB-TEST"
    assert result["ocr"][0]["text"] == "TEST STREET"
    assert result["ocr"][0]["bbox"] == {
        "xMin": 8, "yMin": 10, "xMax": 90, "yMax": 25,
    }
    assert result["objects"][0]["label"] == "person"
    assert result["objects"][0]["box"] == {
        "xMin": 12, "yMin": 16, "xMax": 58, "yMax": 80,
    }
    assert result["image"]["width"] == 128
    assert result["locations"] == []


@pytest.mark.anyio
async def test_result_image_and_review_are_available(app):
    async with asgi_client(app) as client:
        result = (await upload(client)).json()
        request_id = result["requestId"]
        fetched = await client.get(f"/v1/results/{request_id}")
        image = await client.get(result["image"]["url"])
        review = await client.post(
            f"/v1/results/{request_id}/review",
            json={"decision": "confirm", "checkedItems": ["object-0"], "note": ""},
        )

    assert fetched.status_code == 200
    assert fetched.json()["requestId"] == request_id
    assert image.status_code == 200
    assert image.content == png_bytes()
    assert review.status_code == 200
    assert review.json()["decision"] == "confirm"


@pytest.mark.anyio
async def test_invalid_image_is_rejected_without_internal_details(app):
    async with asgi_client(app) as client:
        response = await client.post(
            "/v1/results",
            files={"file": ("invalid.png", b"not-an-image", "image/png")},
        )

    assert response.status_code == 400
    body = response.json()
    assert body["success"] is False
    assert body["error"]["code"] == "INVALID_IMAGE"
    assert "traceback" not in response.text.lower()
