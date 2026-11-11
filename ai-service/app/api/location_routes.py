import asyncio
import logging

from fastapi import APIRouter, Depends, Request

from app.api.deps import get_geocoder, success
from app.core.errors import GeocodingDisabledError, ProcessingTimeoutError
from app.schemas.location_schema import LocationSearchRequest, LocationSearchResult
from app.schemas.prediction_schema import ApiResponse
from app.services.geocoding_provider import GeocodingProvider
from app.services.location_service import search_location

router = APIRouter(prefix="/api/v1/location", tags=["Location"])
logger = logging.getLogger(__name__)


@router.post(
    "/search",
    response_model=ApiResponse[LocationSearchResult],
    summary="Tra cuu dia diem tu chuoi dia chi (chuoi OCR hoac nhap tay) - ket qua chi la goi y",
)
async def search(
    request: Request,
    body: LocationSearchRequest,
    provider: GeocodingProvider = Depends(get_geocoder),
):
    settings = request.app.state.settings
    if not settings.geocoder_enabled:
        raise GeocodingDisabledError("Tính năng tra cứu địa điểm đang bị tắt (AI_GEOCODER_ENABLED=false)")

    def job() -> LocationSearchResult:
        return search_location(
            body.text,
            provider,
            limit=body.limit,
            max_attempts=settings.geocoder_max_attempts,
            budget_s=settings.geocoder_budget_s,
        )

    # Khong dung run_limited: cho I/O mang cua tra cuu khong chiem cac slot AI (OCR/phan tich anh).
    # Moi lan goi ra ngoai da co timeout rieng nen thread khong treo vo han khi request het gio.
    try:
        result = await asyncio.wait_for(asyncio.to_thread(job), timeout=settings.request_timeout_s)
    except asyncio.TimeoutError:
        raise ProcessingTimeoutError(f"Xử lý vượt quá {settings.request_timeout_s:g} giây")
    return success(request, result)
