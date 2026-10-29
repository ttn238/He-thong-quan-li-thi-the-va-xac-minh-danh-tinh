"""Schema cho OCR bien so xe."""
from typing import Optional

from pydantic import BaseModel, Field

from app.schemas.prediction_schema import BoundingBox, ImageInfo


class PlateCandidate(BaseModel):
    raw_text: str = Field(description="Van ban OCR tho")
    plate: Optional[str] = Field(None, description="Bien so da chuan hoa, vi du 29A-123.45")
    is_valid_format: bool = Field(description="Khop dinh dang bien so Viet Nam hay khong")
    confidence: float = Field(ge=0, le=1)
    box: Optional[BoundingBox] = None


class LicensePlateResult(BaseModel):
    image: ImageInfo
    found: bool
    plate: Optional[str] = Field(None, description="Bien so tot nhat (da chuan hoa)")
    plate_compact: Optional[str] = Field(None, description="Bien so khong dau cach/gach, dung de tra cuu, vi du 29A12345")
    province_code: Optional[str] = None
    confidence: float = 0.0
    is_valid_format: bool = False
    candidates: list[PlateCandidate] = []