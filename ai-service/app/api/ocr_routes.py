import logging
from typing import Optional

from fastapi import APIRouter, Depends, Request

from app.api.deps import get_ocr_engine, read_image_bytes, run_limited, success
from app.core.config import get_settings
from app.schemas.ocr_schema import LicensePlateResult
from app.schemas.prediction_schema import ApiResponse
from app.services.ocr_service import OcrEngine, recognize_plate
from app.utils.image_processing import decode_image

router = APIRouter(prefix="/api/v1/ocr", tags=["OCR"])
logger = logging.getLogger(__name__)


@router.post(
    "/license-plate",
    response_model=ApiResponse[LicensePlateResult],
    summary="Nhan dien bien so xe tu anh",
)
async def license_plate(
    request: Request,
    data: Optional[bytes] = Depends(read_image_bytes),
    engine: OcrEngine = Depends(get_ocr_engine),
):
    settings = request.app.state.settings

    def job() -> LicensePlateResult:
        image = decode_image(data, settings)
        return recognize_plate(image, engine, settings.ocr_min_confidence)

    result = await run_limited(request, job)
    return success(request, result)