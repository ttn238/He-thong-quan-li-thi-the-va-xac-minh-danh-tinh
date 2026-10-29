import logging
from typing import Optional

from fastapi import APIRouter, Depends, Request

from app.api.deps import get_detector, read_image_bytes, run_limited, success
from app.schemas.prediction_schema import ApiResponse, ImageAnalysisResult
from app.services.object_detection import ObjectDetector, analyze_image
from app.utils.image_processing import decode_image

router = APIRouter(prefix="/api/v1/image", tags=["Image analysis"])
logger = logging.getLogger(__name__)


@router.post(
    "/analyze",
    response_model=ApiResponse[ImageAnalysisResult],
    summary="Nhan dien nguoi, phuong tien va dac diem nhan dang co ban",
)
async def analyze(
    request: Request,
    data: Optional[bytes] = Depends(read_image_bytes),
    detector: ObjectDetector = Depends(get_detector),
):
    settings = request.app.state.settings

    def job() -> ImageAnalysisResult:
        return analyze_image(decode_image(data, settings), detector)

    result = await run_limited(request, job)
    return success(request, result)