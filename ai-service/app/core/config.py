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

    # Nap san mo hinh luc khoi dong de request dau khong bi cham/timeout
    preload_models: bool = True


@lru_cache
def get_settings() -> Settings:
    return Settings()