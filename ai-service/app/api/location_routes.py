import asyncio
import logging

from typing import Optional

from fastapi import APIRouter, Depends, Request

from app.api.deps import get_geocoder, get_ocr_engine, read_image_bytes, run_limited, success
from app.core.errors import GeocodingDisabledError, ProcessingTimeoutError
from app.schemas.location_schema import DocumentLocationResult, LocationSearchRequest, LocationSearchResult
from app.schemas.prediction_schema import ApiResponse
from app.services.geocoding_provider import GeocodingProvider
from app.services.location_service import search_location
from app.services.ocr_service import OcrEngine, extract_document_info
from app.utils.image_processing import decode_image

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


@router.post(
    "/from-document",
    response_model=ApiResponse[DocumentLocationResult],
    summary="OCR giay to tuy than, lay dia chi (noi cu tru/que quan) roi tra cuu dia diem - chi la goi y",
)
async def from_document(
    request: Request,
    data: Optional[bytes] = Depends(read_image_bytes),
    engine: OcrEngine = Depends(get_ocr_engine),
    provider: GeocodingProvider = Depends(get_geocoder),
):
    settings = request.app.state.settings
    if not settings.geocoder_enabled:
        raise GeocodingDisabledError("Tính năng tra cứu địa điểm đang bị tắt (AI_GEOCODER_ENABLED=false)")

    def job() -> DocumentLocationResult:
        image = decode_image(data, settings)
        doc = extract_document_info(image, engine, settings.ocr_min_confidence)
        for name in ("residence", "hometown"):
            field = getattr(doc, name)
            if field is not None and (field.value or field.raw_value):
                text = (field.value or field.raw_value or "").strip()
                break
        else:
            return DocumentLocationResult(
                document_type=doc.document_type,
                address_found=False,
                warnings=["OCR không đọc được trường địa chỉ (nơi cư trú/quê quán); cần nhập địa chỉ thủ công."],
            )
        warnings = []
        if field.confidence < 0.7:
            warnings.append("Độ tin cậy OCR của địa chỉ thấp; kiểm tra lại chuỗi địa chỉ trước khi đối chiếu.")
        location = search_location(
            text,
            provider,
            limit=5,
            max_attempts=settings.geocoder_max_attempts,
            budget_s=settings.geocoder_budget_s,
        )
        return DocumentLocationResult(
            document_type=doc.document_type,
            source_field=name,
            address_text=text,
            extraction_confidence=field.confidence,
            address_found=True,
            location=location,
            warnings=warnings,
        )

    result = await run_limited(request, job)
    return success(request, result)