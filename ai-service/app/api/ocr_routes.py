"""Ket qua phan tich anh: OCR + YOLO, luu ket qua va anh theo request id."""
import hashlib
import logging
import re
import threading
import uuid
from datetime import datetime, timezone
from pathlib import Path
from typing import Optional

from fastapi import APIRouter, File, Form, HTTPException, Request, UploadFile
from fastapi.responses import FileResponse

from app.api.deps import get_detector, get_ocr_engine, run_limited
from app.core.errors import AppError, ImageTooLargeError, ModelUnavailableError
from app.schemas.prediction_schema import (
    AiResultOut, ImageInfo, MissingField, OcrItem, ReviewIn, ReviewRecord,
)
from app.services.object_detection import analyze_image
from app.utils.image_processing import decode_image

router = APIRouter(prefix="/v1/results", tags=["ai-results"])
logger = logging.getLogger(__name__)
_ID = re.compile(r"^AI-REQ-\d{8}-[0-9A-F]{8}$")
_LOCK = threading.Lock()
_EXT = {"JPEG": "jpg", "PNG": "png", "WEBP": "webp", "BMP": "bmp"}


def _now() -> str:
    return datetime.now(timezone.utc).astimezone().isoformat(timespec="seconds")


def _data_dir(request: Request) -> Path:
    path = request.app.state.settings.data_dir.resolve()
    path.mkdir(parents=True, exist_ok=True)
    return path


def _checked_id(request_id: str) -> str:
    if not _ID.fullmatch(request_id):
        raise HTTPException(400, "Mã yêu cầu không hợp lệ.")
    return request_id


def _load(request: Request, request_id: str) -> AiResultOut:
    path = _data_dir(request) / f"{_checked_id(request_id)}.json"
    if not path.is_file():
        raise HTTPException(404, "Không tìm thấy kết quả phân tích.")
    try:
        return AiResultOut.model_validate_json(path.read_text(encoding="utf-8"))
    except (OSError, ValueError):
        logger.exception("Stored AI result could not be read")
        raise HTTPException(500, "Không đọc được kết quả phân tích.")


def _save(request: Request, result: AiResultOut) -> None:
    path = _data_dir(request) / f"{result.request_id}.json"
    temp = path.with_suffix(".tmp")
    with _LOCK:
        temp.write_text(result.model_dump_json(by_alias=True), encoding="utf-8")
        temp.replace(path)


@router.post("", response_model=AiResultOut, response_model_by_alias=True)
async def analyze(
    request: Request,
    file: UploadFile = File(...),
    linked_record_id: Optional[str] = Form(None),
):
    settings = request.app.state.settings
    data = await file.read(int(settings.max_image_mb * 1024 * 1024) + 1)
    if len(data) > settings.max_image_mb * 1024 * 1024:
        raise ImageTooLargeError(f"Ảnh vượt quá {settings.max_image_mb:g} MB")
    image = decode_image(data, settings)
    request_id = f"AI-REQ-{datetime.now():%Y%m%d}-{uuid.uuid4().hex[:8].upper()}"
    ext = _EXT.get(image_format := _image_format(data), "jpg")
    safe_name = Path(file.filename or f"image.{ext}").name[:180]
    data_dir = _data_dir(request)
    (data_dir / f"{request_id}.{ext}").write_bytes(data)
    image_info = ImageInfo(
        file_name=safe_name, size_bytes=len(data), format=image_format,
        width=image.original_width, height=image.original_height,
        url=f"/v1/results/{request_id}/image",
    )

    detector, ocr_engine = get_detector(), get_ocr_engine()

    def process():
        vision = analyze_image(image, detector)
        text_items: list[OcrItem] = []
        try:
            for line in ocr_engine.read(image.bgr):
                if not line.text.strip():
                    continue
                x1, y1, x2, y2 = image.to_original_box(*line.box)
                text_items.append(OcrItem(
                    text=line.text.strip(), kind="other",
                    confidence=min(1.0, max(0.0, line.confidence)),
                    bbox={"xMin": x1, "yMin": y1, "xMax": x2, "yMax": y2},
                ))
        except Exception:
            logger.exception("OCR failed for request %s", request_id)
        return vision.objects, text_items

    try:
        objects, text_items = await run_limited(request, process)
    except AppError:
        raise
    except Exception as exc:
        logger.exception("Image analysis failed for request %s", request_id)
        raise ModelUnavailableError("Không thể phân tích ảnh lúc này.") from exc

    locations: list[dict] = []
    if settings.geocoder_enabled and text_items:
        try:
            from app.api.deps import get_geocoder
            from app.services.address_normalizer import address_text_from_ocr_lines
            from app.services.location_service import search_location

            raw_lines = [item.text for item in text_items]
            addr_text = address_text_from_ocr_lines(raw_lines, min_confidence=0.5)
            if addr_text.strip():
                loc_res = search_location(
                    addr_text.strip(),
                    get_geocoder(),
                    limit=3,
                    max_attempts=settings.geocoder_max_attempts,
                    budget_s=settings.geocoder_budget_s,
                )
                for i, c in enumerate(loc_res.candidates):
                    admin_area = None
                    if c.address:
                        admin_area = c.address.city or c.address.province or c.address.district
                    locations.append({
                        "rank": i + 1,
                        "name": c.display_name,
                        "confidence": c.similarity,
                        "adminArea": admin_area,
                        "lat": c.lat,
                        "lng": c.lng,
                        "basis": f"Khớp mức {c.match_level}",
                    })
        except Exception:
            logger.debug("Automatic location inference skipped or failed")

    result = AiResultOut(
        request_id=request_id, status="completed", linked_record_id=(linked_record_id or "").strip()[:120] or None,
        submitted_at=_now(), completed_at=_now(), engine="YOLOv8n (COCO) + RapidOCR",
        image_hash=hashlib.sha256(data).hexdigest(), image=image_info,
        ocr=text_items, objects=objects,
        locations=locations,
        missing_fields=[
            MissingField(field="Văn bản/OCR", status="done" if text_items else "missing",
                         recommendation=None if text_items else "Không phát hiện văn bản rõ; kiểm tra ảnh thủ công."),
            MissingField(field="Đối tượng trong ảnh", status="done" if objects else "missing",
                         recommendation=None if objects else "YOLO không phát hiện đối tượng thuộc các lớp hỗ trợ."),
            MissingField(field="Địa điểm suy luận",
                         status="done" if locations else ("unsupported" if not settings.geocoder_enabled else "missing"),
                         recommendation=None if locations else "Không phát hiện chuỗi địa danh hành chính rõ ràng từ OCR."),
        ],
    )
    _save(request, result)
    return result


def _image_path(request: Request, request_id: str) -> Path:
    _load(request, request_id)
    base = _data_dir(request)
    for ext in _EXT.values():
        path = base / f"{request_id}.{ext}"
        if path.is_file():
            return path
    raise HTTPException(404, "Không tìm thấy ảnh.")


@router.get("/{request_id}", response_model=AiResultOut, response_model_by_alias=True)
def get_result(request_id: str, request: Request):
    return _load(request, request_id)


@router.get("/{request_id}/image")
def get_image(request_id: str, request: Request):
    path = _image_path(request, request_id)
    media = {"jpg": "image/jpeg", "png": "image/png", "webp": "image/webp", "bmp": "image/bmp"}[path.suffix[1:]]
    return FileResponse(path, media_type=media)


@router.post("/{request_id}/review", response_model=ReviewRecord, response_model_by_alias=True)
def save_review(request_id: str, body: ReviewIn, request: Request):
    result = _load(request, request_id)
    if result.status != "completed":
        raise HTTPException(409, "Chỉ thẩm định được kết quả đã phân tích xong.")
    if body.decision != "confirm" and not body.note.strip():
        raise HTTPException(400, "Cần ghi chú lý do khi không xác nhận kết quả.")
    result.review = ReviewRecord(**body.model_dump(), reviewed_at=_now())
    _save(request, result)
    return result.review


def _image_format(data: bytes) -> str:
    from io import BytesIO
    from PIL import Image
    with Image.open(BytesIO(data)) as image:
        return (image.format or "JPEG").upper()
