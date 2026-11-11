"""Engine OCR ket hop: RapidOCR (Detector tim hop chu) + VietOCR (Recognizer doc chu tieng Viet).
Trien khai dung giao dien OcrEngine (Protocol) cua ocr_service.
"""
import logging
import threading
from typing import Optional

import cv2
import numpy as np
from PIL import Image

from app.services.ocr_service import OcrLine

logger = logging.getLogger(__name__)


class VietOcrEngine:
    """Engine OCR ho tro tieng Viet chuyen sau, an toan da luong, nap lazy."""

    def __init__(self, model_name: str = "vgg_transformer", device: str = "cpu") -> None:
        self.model_name = model_name
        self.device = device
        self._detector = None
        self._recognizer = None
        self._lock = threading.Lock()

    def load(self) -> None:
        """Nap truoc mo hinh vao bo nho (tranh bi cham o request dau tien)."""
        if self._recognizer is None or self._detector is None:
            with self._lock:
                if self._detector is None:
                    from rapidocr import RapidOCR
                    self._detector = RapidOCR()

                if self._recognizer is None:
                    from vietocr.tool.config import Cfg
                    from vietocr.tool.predictor import Predictor

                    config = Cfg.load_config_from_name(self.model_name)
                    config["device"] = self.device
                    self._recognizer = Predictor(config)
                    logger.info("VietOcrEngine loaded (model=%s, device=%s)", self.model_name, self.device)

    def read(self, bgr: np.ndarray) -> list[OcrLine]:
        """Quet anh tim cac hop chu, sau do dung VietOCR doc chu tieng Viet co dau."""
        self.load()
        h, w = bgr.shape[:2]

        # 1. Dung RapidOCR tim cac hop chu
        out = self._detector(bgr)
        if out is None or out.boxes is None or len(out.boxes) == 0:
            return []

        scores = out.scores if out.scores is not None else [0.9] * len(out.boxes)
        lines: list[OcrLine] = []

        # 2. Cat tung hop chu va dung VietOCR doc
        for score, box in zip(scores, out.boxes):
            xs = [int(p[0]) for p in box]
            ys = [int(p[1]) for p in box]
            x1, x2 = max(0, min(xs)), min(w, max(xs))
            y1, y2 = max(0, min(ys)), min(h, max(ys))

            crop_bgr = bgr[y1:y2, x1:x2]
            if crop_bgr.size == 0 or crop_bgr.shape[0] < 4 or crop_bgr.shape[1] < 4:
                continue

            try:
                crop_pil = Image.fromarray(cv2.cvtColor(crop_bgr, cv2.COLOR_BGR2RGB))
                text = self._recognizer.predict(crop_pil).strip()
            except Exception as e:
                logger.warning("VietOCR predict loi o box (%d, %d, %d, %d): %s", x1, y1, x2, y2, e)
                continue

            if text:
                lines.append(OcrLine(text=text, confidence=float(score), box=(x1, y1, x2, y2)))

        return lines