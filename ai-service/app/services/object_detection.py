"""Nhan dien nguoi va phuong tien bang YOLOv8 (COCO). Cac dac diem nhu hinh xam, seo
can mo hinh huan luyen rieng: dat trong app/models/image_model va mo rong tai day."""
import logging
import threading
from dataclasses import dataclass
from pathlib import Path
from typing import Protocol

import numpy as np

from app.core.errors import ModelUnavailableError
from app.schemas.prediction_schema import BoundingBox, DetectedObject, ImageAnalysisResult, ImageInfo
from app.services.feature_extraction import person_attributes, vehicle_attributes
from app.utils.image_processing import LoadedImage

logger = logging.getLogger(__name__)

# id COCO -> (nhan, nhom)
COCO_CLASSES = {
    0: ("person", "person"),
    1: ("bicycle", "vehicle"),
    2: ("car", "vehicle"),
    3: ("motorcycle", "vehicle"),
    5: ("bus", "vehicle"),
    7: ("truck", "vehicle"),
}


@dataclass
class RawDetection:
    class_id: int
    confidence: float
    box: tuple[float, float, float, float]  # x1, y1, x2, y2 tren anh dau vao


class ObjectDetector(Protocol):
    def detect(self, bgr: np.ndarray) -> list[RawDetection]: ...


class YoloDetector:
    def __init__(self, weights: Path, min_confidence: float, max_objects: int) -> None:
        self._weights = Path(weights)
        self._min_conf = min_confidence
        self._max_det = max_objects
        self._model = None
        self._lock = threading.Lock()

    def load(self) -> None:
        if self._model is None:
            with self._lock:
                if self._model is None:
                    if not self._weights.is_file():
                        raise ModelUnavailableError("Chua co file trong so mo hinh nhan dien")
                    from ultralytics import YOLO

                    self._model = YOLO(str(self._weights))

    def detect(self, bgr: np.ndarray) -> list[RawDetection]:
        self.load()
        res = self._model.predict(
            bgr, conf=self._min_conf, classes=list(COCO_CLASSES), max_det=self._max_det, verbose=False
        )[0]
        out = []
        for b in res.boxes:
            out.append(RawDetection(int(b.cls[0]), float(b.conf[0]), tuple(float(v) for v in b.xyxy[0])))
        return out


def analyze_image(image: LoadedImage, detector: ObjectDetector) -> ImageAnalysisResult:
    detections = detector.detect(image.bgr)
    h, w = image.bgr.shape[:2]
    objects: list[DetectedObject] = []
    for d in sorted(detections, key=lambda d: d.confidence, reverse=True):
        if d.class_id not in COCO_CLASSES:
            continue
        label, category = COCO_CLASSES[d.class_id]
        x1, y1, x2, y2 = (int(max(0, v)) for v in d.box)
        x2, y2 = min(x2, w), min(y2, h)
        attrs = (
            person_attributes(image.bgr, (x1, y1, x2, y2))
            if category == "person"
            else vehicle_attributes(image.bgr, (x1, y1, x2, y2))
        )
        ox1, oy1, ox2, oy2 = image.to_original_box(x1, y1, x2, y2)
        objects.append(
            DetectedObject(
                label=label,
                category=category,
                confidence=round(d.confidence, 4),
                box=BoundingBox(x_min=ox1, y_min=oy1, x_max=ox2, y_max=oy2),
                attributes=attrs,
            )
        )
    summary: dict[str, int] = {}
    for o in objects:
        summary[o.category] = summary.get(o.category, 0) + 1
    logger.info("image_analysis objects=%d", len(objects))
    return ImageAnalysisResult(
        image=ImageInfo(width=image.original_width, height=image.original_height),
        objects=objects,
        summary=summary,
    )
