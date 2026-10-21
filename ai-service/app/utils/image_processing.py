"""Doc, kiem tra va tien xu ly anh dau vao. Khong ghi noi dung/ten file vao log."""
import io
from dataclasses import dataclass
from pathlib import Path
from typing import Optional

import cv2
import numpy as np
from PIL import Image

from app.core.config import Settings
from app.core.errors import (
    ImageNotFoundError,
    ImagePathForbiddenError,
    ImageTooLargeError,
    InvalidImageError,
    MissingImageError,
    UnsupportedMediaError,
)


@dataclass
class LoadedImage:
    """Anh da giai ma (BGR). `scale` = ti le thu nho so voi anh goc (<=1)."""

    bgr: np.ndarray
    original_width: int
    original_height: int
    scale: float

    def to_original_box(self, x1: float, y1: float, x2: float, y2: float) -> tuple[int, int, int, int]:
        s = self.scale or 1.0
        return (
            max(0, int(round(x1 / s))),
            max(0, int(round(y1 / s))),
            min(self.original_width, int(round(x2 / s))),
            min(self.original_height, int(round(y2 / s))),
        )


def read_bytes_from_path(image_path: str, settings: Settings) -> bytes:
    """Doc anh theo duong dan, CHI trong image_base_dir (chan path traversal)."""
    base = settings.image_base_dir
    if base is None:
        raise ImagePathForbiddenError("Che do image_path chua duoc bat tren may chu")
    try:
        base_resolved = Path(base).resolve()
        target = (base_resolved / image_path).resolve()
    except (OSError, ValueError):
        raise InvalidImageError("Duong dan anh khong hop le")
    if base_resolved != target and base_resolved not in target.parents:
        raise ImagePathForbiddenError("Duong dan nam ngoai thu muc cho phep")
    if not target.is_file():
        raise ImageNotFoundError("Khong tim thay anh")
    if target.stat().st_size > settings.max_image_mb * 1024 * 1024:
        raise ImageTooLargeError(f"Anh vuot qua {settings.max_image_mb:g} MB")
    return target.read_bytes()


def decode_image(data: Optional[bytes], settings: Settings) -> LoadedImage:
    """Kiem tra va giai ma anh tu bytes. Nem AppError neu khong hop le."""
    if not data:
        raise MissingImageError("Can gui anh (truong 'file') hoac 'image_path'")
    if len(data) > settings.max_image_mb * 1024 * 1024:
        raise ImageTooLargeError(f"Anh vuot qua {settings.max_image_mb:g} MB")

    # Kiem tra dinh dang that su bang noi dung, khong tin Content-Type/duoi file
    try:
        with Image.open(io.BytesIO(data)) as pil:
            fmt = (pil.format or "").upper()
            w, h = pil.size
    except Exception:
        # Gom ca UnidentifiedImageError/OSError va loi tu cac thu vien da va PIL.Image.open
        # (vi du ultralytics thu nap pi_heif voi tep khong nhan dang duoc)
        raise InvalidImageError("Tep khong phai anh hop le")
    if fmt not in settings.allowed_formats:
        raise UnsupportedMediaError(
            "Dinh dang anh khong ho tro. Cho phep: " + ", ".join(settings.allowed_formats)
        )
    if w * h > settings.max_image_pixels:
        raise ImageTooLargeError("Do phan giai anh qua lon")
    if min(w, h) < settings.min_image_side:
        raise InvalidImageError(f"Anh qua nho (canh ngan nhat toi thieu {settings.min_image_side}px)")

    arr = cv2.imdecode(np.frombuffer(data, np.uint8), cv2.IMREAD_COLOR)
    if arr is None:
        raise InvalidImageError("Khong giai ma duoc anh")
    h, w = arr.shape[:2]

    scale = 1.0
    longest = max(h, w)
    if longest > settings.process_max_side:
        scale = settings.process_max_side / longest
        arr = cv2.resize(arr, (int(w * scale), int(h * scale)), interpolation=cv2.INTER_AREA)
    return LoadedImage(bgr=arr, original_width=w, original_height=h, scale=scale)