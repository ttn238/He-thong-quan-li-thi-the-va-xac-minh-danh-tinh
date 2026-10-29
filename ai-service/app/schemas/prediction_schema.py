from typing import Literal, Optional

from pydantic import BaseModel, ConfigDict, Field
from pydantic.alias_generators import to_camel


class CamelModel(BaseModel):
    """JSON trả về dùng camelCase để khớp frontend (requestId, linkedRecordId...)."""
    model_config = ConfigDict(alias_generator=to_camel, populate_by_name=True)


class OcrItem(CamelModel):
    text: str
    kind: Literal["street_sign", "license_plate", "other"] = "other"
    confidence: float                      # 0..1
    bbox: Optional[list[int]] = None       # [x, y, rộng, cao] theo pixel ảnh gốc
    note: Optional[str] = None


class MissingField(CamelModel):
    field: str
    status: Literal["done", "missing", "out_of_scope", "unsupported"]
    value: Optional[str] = None
    recommendation: Optional[str] = None


class ImageInfo(CamelModel):
    file_name: str
    size_bytes: int
    format: str
    width: int
    height: int
    url: str


class ReviewIn(CamelModel):
    decision: Literal["confirm", "need_more", "reject"]
    checked_items: list[str] = Field(default_factory=list)
    note: str = ""
    reviewer: Optional[str] = None


class ReviewRecord(ReviewIn):
    reviewed_at: str


class AiResultOut(CamelModel):
    request_id: str
    status: Literal["processing", "retrying", "completed", "failed"]
    linked_record_id: Optional[str] = None
    submitted_at: str
    completed_at: Optional[str] = None
    duration_ms: Optional[int] = None
    engine: str
    image_hash: Optional[str] = None
    image: Optional[ImageInfo] = None
    ocr: list[OcrItem] = Field(default_factory=list)
    objects: list[dict] = Field(default_factory=list)
    locations: list[dict] = Field(default_factory=list)
    missing_fields: list[MissingField] = Field(default_factory=list)
    error: Optional[dict] = None
    review: Optional[ReviewRecord] = None