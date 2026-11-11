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


class ExtractedField(BaseModel):
    """Mot truong thong tin trich xuat tu giay to. Truong khong tim thay se KHONG duoc tao
    (None o `DocumentExtractionResult`), khong bao gio dien gia tri doan."""

    value: Optional[str] = Field(None, description="Gia tri da chuan hoa")
    raw_value: Optional[str] = Field(None, description="Van ban OCR tho (da lam sach khoang trang/NFC)")
    confidence: float = Field(0.0, ge=0, le=1)
    box: Optional[BoundingBox] = Field(None, description="Toa do tren anh goc; None neu engine khong tra toa do")


class DocumentExtractionResult(BaseModel):
    image: ImageInfo
    document_type: str = Field(description="cccd | cmnd | driver_license | unknown")
    is_valid_document: bool = Field(description="Nhan dien duoc loai giay to va doc duoc so giay to")
    overall_confidence: float = Field(0.0, ge=0, le=1, description="Trung binh confidence cac truong tim thay")
    id_number: Optional[ExtractedField] = None
    full_name: Optional[ExtractedField] = None
    date_of_birth: Optional[ExtractedField] = None
    gender: Optional[ExtractedField] = None
    nationality: Optional[ExtractedField] = None
    hometown: Optional[ExtractedField] = None
    residence: Optional[ExtractedField] = None
    expiry_date: Optional[ExtractedField] = None
    issue_date: Optional[ExtractedField] = None
    missing_fields: list[str] = Field(default_factory=list, description="Ten cac truong khong nhan dien duoc")
    raw_texts: list[str] = Field(default_factory=list, description="Toan bo van ban OCR da lam sach, theo thu tu doc")
