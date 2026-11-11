"""Schema cho tra cuu dia diem tu chuoi van ban (vi du chuoi OCR hoac dia chi nhap tay).

Ket qua luon la GOI Y: `verified` co dinh la False, nguoi co tham quyen phai doi chieu thuc te.
"""
from typing import Literal, Optional

from pydantic import BaseModel, Field

MatchLevel = Literal["house", "street", "ward", "district", "city", "province", "country", "other"]


class LocationSearchRequest(BaseModel):
    text: str = Field(min_length=1, max_length=300, description="Chuoi dia diem/dia chi can tra cuu")
    limit: int = Field(5, ge=1, le=10, description="So goi y toi da tra ve")


class AddressComponents(BaseModel):
    house_number: Optional[str] = None
    road: Optional[str] = None
    ward: Optional[str] = Field(None, description="Phuong/xa/khu pho (theo du lieu ban do)")
    district: Optional[str] = Field(None, description="Quan/huyen/thi xa (theo du lieu ban do)")
    city: Optional[str] = None
    province: Optional[str] = None
    postcode: Optional[str] = None
    country_code: Optional[str] = None


class LocationCandidate(BaseModel):
    display_name: str = Field(description="Dia chi day du do nguon tra cuu tra ve")
    lat: float = Field(ge=-90, le=90)
    lon: float = Field(ge=-180, le=180)
    match_level: MatchLevel = Field(description="Muc chi tiet cua dia diem tim duoc (so nha, duong, phuong...)")
    similarity: float = Field(ge=0, le=1, description="Do giong giua chuoi truy van va ket qua (uoc luong theo tu khoa)")
    importance: Optional[float] = Field(None, description="Diem quan trong do nguon tra cuu tinh (khong phai do chinh xac)")
    components: AddressComponents = Field(default_factory=AddressComponents)
    source: str = "nominatim"
    osm_url: Optional[str] = Field(None, description="Lien ket doi chieu tren OpenStreetMap")


class LocationSearchResult(BaseModel):
    query: str = Field(description="Chuoi nguoi dung gui len (da cat khoang trang dau/cuoi)")
    normalized_query: str = Field(description="Chuoi sau chuan hoa, dung de tra cuu")
    found: bool
    verified: Literal[False] = Field(False, description="Luon False: ket qua chua duoc xac minh")
    match_scope: Optional[Literal["full", "partial"]] = Field(
        None, description="full: khop theo dia chi day du; partial: da bo bot phan dau (so nha/duong...) moi co ket qua"
    )
    used_query: Optional[str] = Field(None, description="Chuoi thuc su gui cho nguon tra cuu va co ket qua")
    dropped_parts: list[str] = Field(default_factory=list, description="Cac phan da bo khi khop mot phan")
    ambiguous: bool = Field(False, description="True neu co nhieu goi y gan nhu ngang nhau, can nguoi chon")
    candidates: list[LocationCandidate] = Field(default_factory=list)
    warnings: list[str] = Field(default_factory=list)
    attribution: str
    notice: str
