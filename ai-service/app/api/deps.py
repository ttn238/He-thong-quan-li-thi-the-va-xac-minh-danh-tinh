"""Dependency dung chung: lay engine, doc anh dau vao, chay tac vu AI co gioi han
thoi gian va so luong dong thoi."""
import asyncio
import logging
import time
from functools import lru_cache
from typing import Callable, Optional, TypeVar

from fastapi import File, Form, Request, UploadFile

from app.core.config import Settings, get_settings
from app.core.errors import ImageTooLargeError, ProcessingTimeoutError
from app.schemas.prediction_schema import ApiResponse
from app.services.geocoding_provider import GeocodingProvider, NominatimProvider
from app.services.object_detection import ObjectDetector, YoloDetector
from app.services.ocr_service import OcrEngine, RapidOcrEngine
from app.utils.image_processing import read_bytes_from_path

T = TypeVar("T")
logger = logging.getLogger(__name__)


@lru_cache
def get_ocr_engine() -> OcrEngine:
    return RapidOcrEngine()


@lru_cache
def get_geocoder() -> GeocodingProvider:
    """Mot provider dung chung toan tien trinh: gioi han 1 yeu cau/giay va cache chi dung khi dung chung."""
    s = get_settings()
    if "nominatim.openstreetmap.org" in s.geocoder_base_url and "@" not in s.geocoder_user_agent \
            and "http" not in s.geocoder_user_agent:
        logger.warning(
            "AI_GEOCODER_USER_AGENT chua co thong tin lien he; chinh sach Nominatim cong cong yeu cau "
            "User-Agent dinh danh (nen kem email/URL lien he)"
        )
    return NominatimProvider(
        base_url=s.geocoder_base_url,
        user_agent=s.geocoder_user_agent,
        timeout_s=s.geocoder_timeout_s,
        min_interval_s=s.geocoder_min_interval_s,
        country_codes=s.geocoder_country_codes,
        language=s.geocoder_language,
        cache_ttl_s=s.geocoder_cache_ttl_s,
        negative_cache_ttl_s=s.geocoder_negative_cache_ttl_s,
        cache_max_entries=s.geocoder_cache_max_entries,
        cooldown_s=s.geocoder_cooldown_s,
    )


@lru_cache
def get_detector() -> ObjectDetector:
    s = get_settings()
    return YoloDetector(s.detector_weights, s.detector_min_confidence, s.detector_max_objects)


async def read_image_bytes(
    request: Request,
    file: Optional[UploadFile] = File(None, description="Anh tai len (multipart)"),
    image_path: Optional[str] = Form(
        None, description="Duong dan anh tuong doi so voi thu muc AI_IMAGE_BASE_DIR"
    ),
) -> Optional[bytes]:
    """Nhan anh qua `file` HOAC `image_path`. Tra None neu khong co gi (loi xu ly o buoc giai ma)."""
    settings: Settings = request.app.state.settings
    if file is not None:
        limit = int(settings.max_image_mb * 1024 * 1024)
        data = await file.read(limit + 1)  # khong doc qua gioi han vao bo nho
        if len(data) > limit:
            raise ImageTooLargeError(f"Anh vuot qua {settings.max_image_mb:g} MB")
        return data
    if image_path:
        return await asyncio.to_thread(read_bytes_from_path, image_path, settings)
    return None


async def run_limited(request: Request, fn: Callable[..., T], *args) -> T:
    """Chay `fn` trong thread pool, toi da `max_concurrent_jobs` tac vu cung luc va
    `request_timeout_s` giay. Luu y: thread dang chay khong the bi huy giua chung; slot chi
    duoc tra khi thread that su ket thuc, nen timeout khong lam qua tai may chu."""
    settings: Settings = request.app.state.settings
    sem: asyncio.Semaphore = request.app.state.semaphore
    timeout = settings.request_timeout_s
    try:
        await asyncio.wait_for(sem.acquire(), timeout=timeout)
    except asyncio.TimeoutError:
        raise ProcessingTimeoutError("May chu dang ban, vui long thu lai sau")
    fut = asyncio.get_running_loop().run_in_executor(None, fn, *args)
    fut.add_done_callback(lambda _f: sem.release())
    try:
        return await asyncio.wait_for(asyncio.shield(fut), timeout=timeout)
    except asyncio.TimeoutError:
        raise ProcessingTimeoutError(f"Xu ly vuot qua {timeout:g} giay")


def success(request: Request, data) -> ApiResponse:
    elapsed = int((time.perf_counter() - request.state.started) * 1000)
    return ApiResponse(success=True, request_id=request.state.request_id, processing_ms=elapsed, data=data)
