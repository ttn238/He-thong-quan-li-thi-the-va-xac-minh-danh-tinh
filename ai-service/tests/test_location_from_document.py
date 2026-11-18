"""Kiem thu POST /api/v1/location/from-document voi OCR + provider gia (khong goi mang that)."""
from contextlib import asynccontextmanager
from io import BytesIO

import httpx
import numpy as np
import pytest
from PIL import Image

import main
import app.api.location_routes as routes
from app.api.deps import get_geocoder, get_ocr_engine
from app.core.config import get_settings
from app.schemas.ocr_schema import DocumentExtractionResult, ExtractedField
from app.schemas.prediction_schema import ImageInfo
from app.services.geocoding_provider import RawPlace


class FakeProvider:
    name = "fake"
    attribution = "© test"

    def __init__(self, places):
        self.places = places

    def search(self, query, limit):
        return list(self.places)


def doc(residence=None, hometown=None, conf=0.9):
    f = lambda v: ExtractedField(value=v, raw_value=v, confidence=conf) if v else None
    return DocumentExtractionResult(
        image=ImageInfo(width=10, height=10), document_type="cccd", is_valid_document=True,
        residence=f(residence), hometown=f(hometown),
    )


@pytest.fixture
def anyio_backend():
    return "asyncio"


@pytest.fixture
def setup(monkeypatch):
    monkeypatch.setattr(get_settings(), "preload_models", False)
    main.app.dependency_overrides[get_ocr_engine] = lambda: object()
    yield main.app
    main.app.dependency_overrides.clear()


@asynccontextmanager
async def client_for(app):
    async with app.router.lifespan_context(app):
        async with httpx.AsyncClient(transport=httpx.ASGITransport(app=app), base_url="http://test") as c:
            yield c


def png() -> bytes:
    out = BytesIO()
    Image.fromarray(np.full((64, 64, 3), 150, dtype=np.uint8)).save(out, format="PNG")
    return out.getvalue()


async def call(app):
    async with client_for(app) as c:
        return await c.post("/api/v1/location/from-document", files={"file": ("a.png", png(), "image/png")})


@pytest.mark.anyio
async def test_uses_residence_and_returns_unverified_suggestions(setup, monkeypatch):
    app = setup
    monkeypatch.setattr(routes, "extract_document_info", lambda *a, **k: doc("12 Nguyễn Huệ, Quận 1, TPHCM", "Hà Nội"))
    hit = RawPlace(display_name="12, Nguyễn Huệ, Quận 1, Thành phố Hồ Chí Minh, Việt Nam", lat=10.77, lon=106.70,
                   addresstype="building", importance=0.3, address={"road": "Nguyễn Huệ", "country_code": "vn"},
                   osm_type="way", osm_id=1)
    app.dependency_overrides[get_geocoder] = lambda: FakeProvider([hit])
    r = await call(app)
    data = r.json()["data"]
    assert r.status_code == 200
    assert data["source_field"] == "residence" and data["address_found"] is True
    assert data["location"]["found"] is True and data["location"]["verified"] is False


@pytest.mark.anyio
async def test_falls_back_to_hometown(setup, monkeypatch):
    app = setup
    monkeypatch.setattr(routes, "extract_document_info", lambda *a, **k: doc(None, "Hà Nội"))
    app.dependency_overrides[get_geocoder] = lambda: FakeProvider([])
    data = (await call(app)).json()["data"]
    assert data["source_field"] == "hometown"
    assert data["location"]["found"] is False  # khong co ket qua: khong khang dinh dia diem


@pytest.mark.anyio
async def test_no_address_in_document(setup, monkeypatch):
    app = setup
    monkeypatch.setattr(routes, "extract_document_info", lambda *a, **k: doc())
    app.dependency_overrides[get_geocoder] = lambda: FakeProvider([])
    data = (await call(app)).json()["data"]
    assert data["address_found"] is False and data["location"] is None and data["warnings"]
