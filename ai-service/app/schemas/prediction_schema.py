from datetime import datetime
from typing import Annotated, Generic, Literal, Optional

from pydantic import BaseModel, ConfigDict, Field
from pydantic.alias_generators import to_camel

T = TypeVar("T")

class CamelModel(BaseModel):
    """JSON trả về dùng camelCase để khớp frontend (requestId, linkedRecordId...)."""
    model_config = ConfigDict(alias_generator=to_camel, populate_by_name=True)

class ErrorBody(BaseModel):
    code: str
    message: str

class ApiResponse(BaseModel, Generic[T]):
    """Dinh dang phan hoi thong nhat cho moi endpoint."""
    success: bool
    request_id: str
    processing_ms: int
    data: Optional[T] = None
    error: Optional[ErrorBody] = None

class BoundingBox(CamelModel):
    """Pixel trên ảnh GỐC."""
    x_min: int
    y_min: int
    x_max: int
    y_max: int

Confidence = Annotated[float, Field(ge=0, le=1)]

class OcrItem(CamelModel):
    text: str
    kind: Literal["street_sign", "license_plate", "other"] = "other"
    confidence: Confidence
    bbox: Optional[BoundingBox] = None
    note: Optional[str] = None

class DetectedObject(CamelModel):
    label: Literal["person", "car", "motorcycle", "bus", "truck", "bicycle"]
    category: Literal["person", "vehicle"]
    confidence: Confidence
    box: BoundingBox
    attributes: dict[str, str] = Field(default_factory=dict)

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
    submitted_at: datetime
    completed_at: Optional[datetime] = None
    duration_ms: Optional[int] = None
    engine: str
    image_hash: Optional[str] = None
    image: Optional[ImageInfo] = None
    ocr: list[OcrItem] = Field(default_factory=list)
    objects: list[DetectedObject] = Field(default_factory=list)
    locations: list[dict] = Field(default_factory=list)
    missing_fields: list[MissingField] = Field(default_factory=list)
    error: Optional[ErrorBody] = None
    review: Optional[ReviewRecord] = None