import re
import time
import uuid
from datetime import datetime, timezone
from pathlib import Path
from typing import Optional

from fastapi import APIRouter, File, Form, HTTPException, UploadFile
from fastapi.responses import FileResponse

from app.core.config import DATA_DIR, ENGINE_NAME, MAX_UPLOAD_BYTES
from app.schemas.prediction_schema import (
    AiResultOut, ImageInfo, MissingField, ReviewIn, ReviewRecord,
)
from app.services.ocr_service import run_ocr
from app.utils.image_processing import EXT, MEDIA, read_and_validate, sha256_hex

router = APIRouter(prefix="/v1/results", tags=["ai-results"])

_ID = re.compile(r"^AI-REQ-\d{8}-[0-9A-F]{8}$")   # chặn path traversal


def _now() -> str:
    return datetime.now(timezone.utc).astimezone().isoformat(timespec="seconds")


def _check_id(request_id: str) -> str:
    if not _ID.match(request_id):
        raise HTTPException(400, "Mã yêu cầu không hợp lệ.")
    return request_id


def _json_path(request_id: str) -> Path:
    return DATA_DIR / f"{request_id}.json"


def _save(result: AiResultOut) -> None:
    path = _json_path(result.request_id)
    tmp = path.with_suffix(".tmp")
    tmp.write_text(result.model_dump_json(by_alias=True), encoding="utf-8")
    tmp.replace(path)


def _load(request_id: str) -> AiResultOut:
    path = _json_path(_check_id(request_id))
    if not path.exists():
        raise HTTPException(404, "Không tìm thấy kết quả phân tích.")
    return AiResultOut.model_validate_json(path.read_text(encoding="utf-8"))


def _find_image(request_id: str) -> Path:
    for ext in EXT.values():
        p = DATA_DIR / f"{request_id}.{ext}"
        if p.exists():
            return p
    raise HTTPException(404, "Không tìm thấy ảnh.")


@router.post("", response_model=AiResultOut, response_model_by_alias=True)
def analyze(file: UploadFile = File(...), linked_record_id: Optional[str] = Form(None)):
    data = file.file.read(MAX_UPLOAD_BYTES + 1)
    img, fmt = read_and_validate(data)

    request_id = f"AI-REQ-{datetime.now():%Y%m%d}-{uuid.uuid4().hex[:8].upper()}"
    ext = EXT[fmt]
    (DATA_DIR / f"{request_id}.{ext}").write_bytes(data)

    result = AiResultOut(
        request_id=request_id,
        status="processing",
        linked_record_id=(linked_record_id or "").strip() or None,
        submitted_at=_now(),
        engine=ENGINE_NAME,
        image_hash=sha256_hex(data),
        image=ImageInfo(
            file_name=Path(file.filename or f"anh.{ext}").name,
            size_bytes=len(data), format=fmt, width=img.width, height=img.height,
            url=f"/v1/results/{request_id}/image",
        ),
    )

    started = time.perf_counter()
    try:
        items = run_ocr(img)
        plate = next((i for i in items if i.kind == "license_plate"), None)
        result.ocr = items
        result.missing_fields = [
            MissingField(
                field="Biển số phương tiện (OCR)",
                status="done" if plate else "missing",
                value=f"{plate.text} ({plate.confidence * 100:.1f}%)" if plate else None,
                recommendation=None if plate else "Không đọc được biển số; chụp bổ sung ảnh cận biển số.",
            ),
            MissingField(
                field="Nhận diện đối tượng / đặc điểm",
                status="unsupported",
                recommendation="Chưa tích hợp mô hình nhận diện đối tượng; cán bộ mô tả thủ công.",
            ),
            MissingField(
                field="Địa điểm suy luận",
                status="unsupported",
                recommendation="Chưa tích hợp dữ liệu bản đồ; cán bộ xác định địa điểm thủ công.",
            ),
        ]
        result.status = "completed"
    except Exception as exc:  # noqa: BLE001 - lưu lỗi để giao diện hiển thị trạng thái lỗi
        result.status = "failed"
        result.error = {
            "code": "ERR_AI_OCR_FAILED",
            "message": "Không thể phân tích ảnh bằng động cơ OCR.",
            "traceId": uuid.uuid4().hex[:16],
            "occurredAt": _now(),
        }
        print(f"[ai-service] OCR failed for {request_id}: {exc!r}")
    result.completed_at = _now()
    result.duration_ms = int((time.perf_counter() - started) * 1000)
    _save(result)
    return result


@router.get("/{request_id}", response_model=AiResultOut, response_model_by_alias=True)
def get_result(request_id: str):
    return _load(request_id)


@router.get("/{request_id}/image")
def get_image(request_id: str):
    _load(request_id)  # kiểm tra id hợp lệ + tồn tại
    path = _find_image(request_id)
    return FileResponse(path, media_type=MEDIA[path.suffix.lstrip(".")])


@router.post("/{request_id}/review", response_model=ReviewRecord, response_model_by_alias=True)
def save_review(request_id: str, body: ReviewIn):
    result = _load(request_id)
    if result.status != "completed":
        raise HTTPException(409, "Chỉ thẩm định được kết quả đã phân tích xong.")
    if body.decision != "confirm" and not body.note.strip():
        raise HTTPException(400, "Cần ghi chú lý do khi không xác nhận kết quả.")
    result.review = ReviewRecord(**body.model_dump(), reviewed_at=_now())
    _save(result)
    return result.review