"""OCR bien so xe: engine OCR (co the thay the/gia lap khi test) + dieu phoi
dinh vi -> tang cuong -> OCR -> chuan hoa -> chon ket qua tot nhat."""
import logging
import threading
from dataclasses import dataclass
from typing import Optional, Protocol

import numpy as np

from app.schemas.ocr_schema import LicensePlateResult, PlateCandidate
from app.schemas.prediction_schema import BoundingBox, ImageInfo
from app.services.image_enhancement import enhance_for_ocr
from app.services.plate_locator import locate_plate_regions
from app.services.plate_normalizer import NormalizedPlate, normalize_plate
from app.utils.image_processing import LoadedImage

logger = logging.getLogger(__name__)


@dataclass
class OcrLine:
    text: str
    confidence: float
    box: tuple[float, float, float, float]  # x1, y1, x2, y2 trong he toa do cua anh dua vao engine


class OcrEngine(Protocol):
    def read(self, bgr: np.ndarray) -> list[OcrLine]: ...


class RapidOcrEngine:
    """Engine OCR chay bang ONNX Runtime (RapidOCR). Nap lazy, an toan da luong."""

    def __init__(self) -> None:
        self._engine = None
        self._lock = threading.Lock()

    def load(self) -> None:
        if self._engine is None:
            with self._lock:
                if self._engine is None:
                    from rapidocr import RapidOCR  # import tre de khoi dong nhanh

                    self._engine = RapidOCR()

    def read(self, bgr: np.ndarray) -> list[OcrLine]:
        self.load()
        out = self._engine(bgr)
        if out is None or out.txts is None or out.boxes is None:
            return []
        lines = []
        for txt, score, box in zip(out.txts, out.scores, out.boxes):
            xs, ys = [p[0] for p in box], [p[1] for p in box]
            lines.append(OcrLine(str(txt), float(score), (min(xs), min(ys), max(xs), max(ys))))
        return lines


@dataclass
class _Cand:
    raw: str
    norm: NormalizedPlate
    confidence: float
    box: Optional[tuple[int, int, int, int]]  # toa do tren anh DA THU NHO


def _candidates_from_lines(
    lines: list[OcrLine], offset: tuple[int, int], crop_scale: float, min_conf: float
) -> list[_Cand]:
    """Tao ung vien tu cac dong OCR: tung dong rieng le va ghep cac dong tu tren xuong
    (bien 2 dong). Toa do duoc quy ve anh dau vao (cong offset, chia ti le crop)."""
    lines = [l for l in lines if l.confidence >= min_conf and l.text.strip()]
    if not lines:
        return []
    lines = sorted(lines, key=lambda l: (l.box[1] + l.box[3]) / 2)

    def to_img(b):
        return (
            int(b[0] / crop_scale) + offset[0],
            int(b[1] / crop_scale) + offset[1],
            int(b[2] / crop_scale) + offset[0],
            int(b[3] / crop_scale) + offset[1],
        )

    def union(group):
        bs = [to_img(l.box) for l in group]
        return (min(b[0] for b in bs), min(b[1] for b in bs), max(b[2] for b in bs), max(b[3] for b in bs))

    groups: list[list[OcrLine]] = [[l] for l in lines]
    if len(lines) > 1:
        groups.append(lines)  # bien 2 dong
        for i in range(len(lines) - 1):
            groups.append(lines[i : i + 2])
    seen, cands = set(), []
    for g in groups:
        raw = " ".join(l.text for l in g)
        if raw in seen:
            continue
        seen.add(raw)
        conf = sum(l.confidence for l in g) / len(g)
        cands.append(_Cand(raw, normalize_plate(raw), conf, union(g)))
    return cands


def _rank_key(c: _Cand):
    # Hop le truoc; it sua loi; confidence cao
    return (not c.norm.is_valid_format, c.norm.corrections, -c.confidence)


def recognize_plate(
    image: LoadedImage, engine: OcrEngine, min_confidence: float = 0.5
) -> LicensePlateResult:
    bgr = image.bgr
    h, w = bgr.shape[:2]
    all_cands: list[_Cand] = []

    def good_enough(cs: list[_Cand]) -> bool:
        return any(c.norm.is_valid_format and c.norm.corrections == 0 and c.confidence >= 0.8 for c in cs)

    regions = locate_plate_regions(bgr)
    for (x1, y1, x2, y2) in regions:
        crop = bgr[y1:y2, x1:x2]
        if crop.size == 0:
            continue
        enhanced = enhance_for_ocr(crop)
        scale = enhanced.shape[0] / crop.shape[0]
        all_cands += _candidates_from_lines(engine.read(enhanced), (x1, y1), scale, min_confidence)
        if good_enough(all_cands):
            break

    if not good_enough(all_cands):
        # Du phong: OCR toan anh (bien so co the khong duoc dinh vi bang hinh hoc)
        all_cands += _candidates_from_lines(engine.read(enhance_for_ocr(bgr)), (0, 0), 1.0, min_confidence)

    valid = sorted((c for c in all_cands if c.norm.is_valid_format), key=_rank_key)
    # Loai trung cung bien so, giu ban tot nhat
    uniq: list[_Cand] = []
    seen_plates = set()
    for c in valid:
        if c.norm.compact not in seen_plates:
            seen_plates.add(c.norm.compact)
            uniq.append(c)
    shown = uniq[:5] if uniq else sorted(all_cands, key=_rank_key)[:3]

    def to_model(c: _Cand) -> PlateCandidate:
        box = None
        if c.box:
            x1, y1, x2, y2 = image.to_original_box(*c.box)
            box = BoundingBox(x_min=x1, y_min=y1, x_max=x2, y_max=y2)
        return PlateCandidate(
            raw_text=c.raw,
            plate=c.norm.plate,
            is_valid_format=c.norm.is_valid_format,
            confidence=round(min(max(c.confidence, 0.0), 1.0), 4),
            box=box,
        )

    info = ImageInfo(width=image.original_width, height=image.original_height)
    if not uniq:
        logger.info("plate_ocr found=false regions=%d candidates=%d", len(regions), len(all_cands))
        return LicensePlateResult(image=info, found=False, candidates=[to_model(c) for c in shown])

    best = uniq[0]
    logger.info("plate_ocr found=true regions=%d candidates=%d", len(regions), len(all_cands))
    return LicensePlateResult(
        image=info,
        found=True,
        plate=best.norm.plate,
        plate_compact=best.norm.compact,
        province_code=best.norm.province_code,
        confidence=round(min(max(best.confidence, 0.0), 1.0), 4),
        is_valid_format=True,
        candidates=[to_model(c) for c in shown],
    )