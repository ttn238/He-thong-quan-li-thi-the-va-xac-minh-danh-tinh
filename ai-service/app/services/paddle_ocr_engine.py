"""Engine OCR dung PaddleOCR (task 5.6), cam vao giao dien `OcrEngine` cua ocr_service.

- Chon bang bien moi truong AI_OCR_ENGINE=paddle (mac dinh van la rapidocr).
- Import `paddleocr` tre: import module nay khong keo paddle va khong cham mang.
  Neu nap that bai (thieu thu vien, khong tai duoc mo hinh...) -> ModelUnavailableError (503),
  khong lam sap ung dung; sau loi se cho `retry_cooldown_s` giay moi thu nap lai.
- Khong tu tao confidence/toa do: dong nao PaddleOCR khong tra du van ban + diem + da giac thi bi bo.
- PaddleOCR khong duoc dam bao an toan da luong -> suy luan duoc tuan tu hoa bang khoa.
- Khong log noi dung van ban nhan dang (co the la thong tin ca nhan).
"""
import logging
import threading
import time
from collections.abc import Mapping
from typing import Any, Optional

import numpy as np

from app.core.errors import ModelUnavailableError, OcrFailedError

logger = logging.getLogger(__name__)

# (van ban, diem tin cay hoac None, da giac [[x, y], ...] hoac None) theo he toa do anh dua vao engine
RawItem = tuple[str, Optional[float], Optional[list[list[float]]]]


def _score(value: Any) -> Optional[float]:
    try:
        s = float(value)
    except (TypeError, ValueError):
        return None
    if not np.isfinite(s):
        return None
    return min(1.0, max(0.0, s))


def _polygon(poly: Any) -> Optional[list[list[float]]]:
    if poly is None:
        return None
    try:
        arr = np.asarray(poly, dtype=np.float64)
    except (TypeError, ValueError):
        return None
    if arr.ndim != 2 or arr.shape[1] != 2 or arr.shape[0] < 3 or not np.all(np.isfinite(arr)):
        return None
    return arr.tolist()


def parse_v3(raw: Any) -> list[RawItem]:
    """Dau ra PaddleOCR 3.x (`predict()`): doi tuong dang dict co rec_texts / rec_scores / rec_polys."""
    items: list[RawItem] = []
    for page in raw or []:
        data: Any = page
        if not isinstance(data, Mapping) and hasattr(data, "json"):
            data = data.json
        if not isinstance(data, Mapping):
            continue
        if isinstance(data.get("res"), Mapping):
            data = data["res"]
        texts = data.get("rec_texts")
        if texts is None:
            continue
        scores = data.get("rec_scores")
        polys = data.get("rec_polys")
        if polys is None:
            polys = data.get("dt_polys")
        texts = list(texts)
        scores = list(scores) if scores is not None else []
        polys = list(polys) if polys is not None else []
        for i, text in enumerate(texts):
            text = str(text).strip()
            if text:
                items.append((text, _score(scores[i]) if i < len(scores) else None,
                              _polygon(polys[i]) if i < len(polys) else None))
    return items


def parse_v2(raw: Any) -> list[RawItem]:
    """Dau ra PaddleOCR 2.x (`ocr()`): [[ [box, (text, score)], ... ]]."""
    items: list[RawItem] = []
    for page in raw or []:
        if not page:
            continue
        for entry in page:
            try:
                box, (text, score) = entry[0], entry[1]
            except (TypeError, ValueError, IndexError, KeyError):
                continue
            text = str(text).strip()
            if text:
                items.append((text, _score(score), _polygon(box)))
    return items


def polygon_to_xyxy(poly: list[list[float]]) -> tuple[float, float, float, float]:
    xs, ys = [p[0] for p in poly], [p[1] for p in poly]
    return (min(xs), min(ys), max(xs), max(ys))


class PaddleOcrEngine:
    """Engine OCR PaddleOCR. Giao dien: `load()` va `read(bgr) -> list[OcrLine]`."""

    def __init__(
        self,
        lang: str = "en",
        device: str = "cpu",
        det_model: Optional[str] = None,
        rec_model: Optional[str] = None,
        retry_cooldown_s: float = 30.0,
    ) -> None:
        self.lang = lang
        self.device = device
        self.det_model = det_model
        self.rec_model = rec_model
        self.retry_cooldown_s = retry_cooldown_s
        self._engine: Any = None
        self._major = 3
        self._version: Optional[str] = None
        self._load_lock = threading.Lock()
        self._infer_lock = threading.Lock()
        self._failed_at: Optional[float] = None
        self._failed_msg: Optional[str] = None

    # ---- nap mo hinh ----
    def load(self) -> None:
        if self._engine is not None:
            return
        with self._load_lock:
            if self._engine is not None:
                return
            if self._failed_at is not None and time.monotonic() - self._failed_at < self.retry_cooldown_s:
                raise ModelUnavailableError(
                    f"PaddleOCR chua kha dung (loi gan nhat: {self._failed_msg}). Thu lai sau it giay"
                )
            try:
                self._engine = self._build()
            except Exception as exc:  # noqa: BLE001 - moi loi nap deu quy ve "mo hinh khong kha dung"
                self._failed_at = time.monotonic()
                self._failed_msg = f"{type(exc).__name__}: {str(exc)[:160]}"
                logger.warning("khong nap duoc PaddleOCR: %s", self._failed_msg)
                raise ModelUnavailableError(
                    f"Khong nap duoc PaddleOCR ({self._failed_msg}). "
                    "Kiem tra da cai paddleocr/paddlepaddle va may tai duoc mo hinh lan dau"
                ) from exc

    def _build(self) -> Any:
        from importlib import metadata

        from paddleocr import PaddleOCR  # import tre

        try:
            self._version = metadata.version("paddleocr")
            self._major = int(self._version.split(".")[0])
        except (metadata.PackageNotFoundError, ValueError):
            self._version, self._major = None, 3

        if self._major >= 3:
            kwargs: dict[str, Any] = dict(
                lang=self.lang,
                device=self.device,
                use_doc_orientation_classify=False,  # giu nguyen he toa do anh dau vao
                use_doc_unwarping=False,
                use_textline_orientation=True,
            )
            if self.det_model:
                kwargs["text_detection_model_name"] = self.det_model
            if self.rec_model:
                kwargs["text_recognition_model_name"] = self.rec_model
            return PaddleOCR(**kwargs)
        return PaddleOCR(lang=self.lang, use_angle_cls=True, use_gpu=self.device.lower().startswith("gpu"),
                         show_log=False)

    # ---- suy luan ----
    def read(self, bgr: np.ndarray) -> list:
        from app.services.ocr_service import OcrLine  # import tre: tranh vong lap/keo schema khi chi can parser

        self.load()
        try:
            with self._infer_lock:
                if self._major >= 3:
                    items = parse_v3(self._engine.predict(bgr))
                else:
                    items = parse_v2(self._engine.ocr(bgr, cls=True))
        except Exception as exc:  # noqa: BLE001
            logger.error("PaddleOCR suy luan that bai: %s", type(exc).__name__)  # khong log noi dung
            raise OcrFailedError("OCR that bai") from exc

        lines, dropped = [], 0
        for text, conf, poly in items:
            if conf is None or poly is None:
                dropped += 1  # khong tu tao confidence/toa do
                continue
            lines.append(OcrLine(text, conf, polygon_to_xyxy(poly)))
        if dropped:
            logger.warning("bo %d dong do PaddleOCR khong tra du diem tin cay/toa do", dropped)
        return lines

    def status(self) -> dict:
        state = "loaded" if self._engine is not None else ("unavailable" if self._failed_at else "not_loaded")
        return {"engine": "paddleocr", "state": state, "version": self._version, "lang": self.lang,
                "device": self.device, "last_error": self._failed_msg}
