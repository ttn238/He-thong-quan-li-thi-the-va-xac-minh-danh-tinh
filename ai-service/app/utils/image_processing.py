import hashlib
import io

from fastapi import HTTPException
from PIL import Image, UnidentifiedImageError

from app.core.config import ALLOWED_FORMATS, MAX_IMAGE_PIXELS, MAX_UPLOAD_BYTES

Image.MAX_IMAGE_PIXELS = MAX_IMAGE_PIXELS

EXT = {"JPEG": "jpg", "PNG": "png", "WEBP": "webp", "BMP": "bmp"}
MEDIA = {"jpg": "image/jpeg", "png": "image/png", "webp": "image/webp", "bmp": "image/bmp"}


def read_and_validate(data: bytes) -> tuple[Image.Image, str]:
    """Kiểm tra dung lượng và NỘI DUNG ảnh thật (không tin đuôi file). Trả về (ảnh, định dạng)."""
    if not data:
        raise HTTPException(400, "Tệp rỗng.")
    if len(data) > MAX_UPLOAD_BYTES:
        raise HTTPException(413, "Ảnh vượt quá 10 MB.")
    try:
        with Image.open(io.BytesIO(data)) as probe:
            probe.verify()
        img = Image.open(io.BytesIO(data))
        img.load()
    except (UnidentifiedImageError, OSError, Image.DecompressionBombError):
        raise HTTPException(400, "Tệp không phải ảnh hợp lệ hoặc bị hỏng.")
    fmt = (img.format or "").upper()
    if fmt not in ALLOWED_FORMATS:
        raise HTTPException(400, "Chỉ hỗ trợ JPG, PNG, WEBP, BMP.")
    return img, fmt


def sha256_hex(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()