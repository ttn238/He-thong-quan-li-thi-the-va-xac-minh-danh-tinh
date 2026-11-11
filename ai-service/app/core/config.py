"""Cau hinh he thong. Moi gia tri co the ghi de bang bien moi truong tien to AI_
hoac file .env (vi du AI_MAX_IMAGE_MB=5)."""
from functools import lru_cache
from pathlib import Path
from typing import Optional

from pydantic import Field
from pydantic_settings import BaseSettings, SettingsConfigDict

BASE_DIR = Path(__file__).resolve().parent.parent  # thu muc app/


class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_prefix="AI_", env_file=".env", extra="ignore")

    app_name: str = "ai-service"
    app_version: str = "1.0.0"
    log_level: str = "INFO"

    # --- Dau vao anh ---
    max_image_mb: float = Field(10.0, gt=0, description="Dung luong anh toi da (MB)")
    max_image_pixels: int = Field(40_000_000, gt=0, description="So pixel toi da (chong decompression bomb)")
    min_image_side: int = Field(32, gt=0, description="Canh ngan nhat cho phep (px)")
    process_max_side: int = Field(1600, gt=0, description="Canh dai toi da khi xu ly, anh lon hon se duoc thu nho")
    allowed_formats: tuple[str, ...] = ("JPEG", "PNG", "WEBP", "BMP")

    # Thu muc goc cho phep doc anh theo duong dan. De trong = tat che do image_path.
    image_base_dir: Optional[Path] = None

    # --- Gioi han thoi gian / tai ---
    request_timeout_s: float = Field(20.0, gt=0, description="Thoi gian xu ly toi da cho 1 request")
    max_concurrent_jobs: int = Field(2, ge=1, description="So tac vu AI chay dong thoi")

    # --- OCR ---
    ocr_min_confidence: float = Field(0.5, ge=0, le=1)

    # --- Nhan dien doi tuong ---
    detector_weights: Path = BASE_DIR / "models" / "image_model" / "yolov8n.pt"
    detector_min_confidence: float = Field(0.35, ge=0, le=1)
    detector_max_objects: int = Field(50, ge=1)

    # --- Tra cuu dia diem (Nominatim/OpenStreetMap, mien phi) ---
    # Chinh sach Nominatim cong cong: <= 1 yeu cau/giay, User-Agent dinh danh (nen kem lien he), phai cache,
    # khong gui du lieu ca nhan/mat. Dat geocoder_enabled=false de tat; doi geocoder_base_url de dung may chu rieng.
    geocoder_enabled: bool = True
    geocoder_base_url: str = "https://nominatim.openstreetmap.org"
    geocoder_user_agent: str = "DeathManagement-AI-Service/1.0"
    geocoder_timeout_s: float = Field(5.0, gt=0, description="Timeout moi lan goi nguon tra cuu")
    geocoder_min_interval_s: float = Field(1.0, ge=0, description="Khoang cach toi thieu giua 2 lan goi ra ngoai")
    geocoder_country_codes: str = "vn"
    geocoder_language: str = "vi"
    geocoder_max_attempts: int = Field(3, ge=1, le=5, description="So bien the (day du -> rut gon) thu toi da")
    geocoder_budget_s: float = Field(12.0, gt=0, description="Tong thoi gian toi da de thu cac bien the rut gon")
    geocoder_cache_ttl_s: float = Field(86400.0, ge=0)
    geocoder_negative_cache_ttl_s: float = Field(300.0, ge=0, description="Cache ket qua 'khong tim thay'")
    geocoder_cache_max_entries: int = Field(1000, ge=1)
    geocoder_cooldown_s: float = Field(60.0, ge=0, description="Tam ngung goi sau loi mang/429/403/5xx")

    # Nap san mo hinh luc khoi dong de request dau khong bi cham/timeout
    preload_models: bool = True


@lru_cache
def get_settings() -> Settings:
    return Settings()