"""Schema dung chung (envelope) va schema phan tich hinh anh."""
from typing import Generic, Optional, TypeVar

from pydantic import BaseModel, Field

T = TypeVar("T")


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


class ImageInfo(BaseModel):
    width: int
    height: int


class BoundingBox(BaseModel):
    """Toa do pixel tren anh GOC (khong phai anh da thu nho)."""

    x_min: int
    y_min: int
    x_max: int
    y_max: int


class DetectedObject(BaseModel):
    label: str = Field(description="person | car | motorcycle | bus | truck | bicycle")
    category: str = Field(description="person | vehicle")
    confidence: float = Field(ge=0, le=1)
    box: BoundingBox
    attributes: dict[str, str] = Field(
        default_factory=dict,
        description="Dac diem nhan dang uoc luong, vi du upper_color/lower_color cua nguoi",
    )


class ImageAnalysisResult(BaseModel):
    image: ImageInfo
    objects: list[DetectedObject]
    summary: dict[str, int] = Field(description="So luong theo category")