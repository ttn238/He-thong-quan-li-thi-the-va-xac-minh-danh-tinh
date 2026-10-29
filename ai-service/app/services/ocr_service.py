import re
import unicodedata

import numpy as np
from PIL import Image
from rapidocr_onnxruntime import RapidOCR

from app.schemas.prediction_schema import OcrItem

_engine: RapidOCR | None = None

# Biển số VN, ví dụ: 29B1-884.21, 30A-123.45, 59-C1 12345
_PLATE = re.compile(r"^\d{2}[-\s]?[A-Z]{1,2}\d?[-\s]?(\d{3}\.\d{2}|\d{4,5})$")
_STREET_PREFIX = ("DUONG ", "PHO ", "NGO ", "NGACH ", "QUOC LO", "DAI LO")
LOW_CONFIDENCE = 0.8


def _engine_instance() -> RapidOCR:
    global _engine
    if _engine is None:           # nạp mô hình một lần (mất vài giây)
        _engine = RapidOCR()
    return _engine


def _strip_accents(s: str) -> str:
    s = s.replace("Đ", "D").replace("đ", "d")
    return "".join(c for c in unicodedata.normalize("NFD", s) if unicodedata.category(c) != "Mn")


def _kind(text: str) -> str:
    t = text.strip().upper()
    if _PLATE.match(t):
        return "license_plate"
    if _strip_accents(t).startswith(_STREET_PREFIX):
        return "street_sign"
    return "other"


def run_ocr(img: Image.Image) -> list[OcrItem]:
    arr = np.array(img.convert("RGB"))[:, :, ::-1]      # RGB -> BGR
    result, _ = _engine_instance()(arr)
    items: list[OcrItem] = []
    for box, text, score in result or []:
        text = str(text).strip()
        if not text:
            continue
        xs = [p[0] for p in box]
        ys = [p[1] for p in box]
        x, y = int(min(xs)), int(min(ys))
        conf = round(float(score), 4)
        items.append(OcrItem(
            text=text,
            kind=_kind(text),
            confidence=conf,
            bbox=[x, y, int(max(xs)) - x, int(max(ys)) - y],
            note="Độ tin cậy thấp, cần cán bộ thẩm tra" if conf < LOW_CONFIDENCE else None,
        ))
    return items