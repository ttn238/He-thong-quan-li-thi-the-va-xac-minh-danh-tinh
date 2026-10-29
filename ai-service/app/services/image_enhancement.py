"""Tang cuong anh truoc OCR (anh hien truong thuong toi, mo, nho)."""
import cv2
import numpy as np


def enhance_for_ocr(bgr: np.ndarray, target_min_height: int = 96) -> np.ndarray:
    """Phong to neu vung anh nho, tang tuong phan cuc bo (CLAHE)."""
    h = bgr.shape[0]
    if h < target_min_height:
        f = target_min_height / h
        bgr = cv2.resize(bgr, None, fx=f, fy=f, interpolation=cv2.INTER_CUBIC)
    lab = cv2.cvtColor(bgr, cv2.COLOR_BGR2LAB)
    l, a, b = cv2.split(lab)
    l = cv2.createCLAHE(clipLimit=2.0, tileGridSize=(8, 8)).apply(l)
    return cv2.cvtColor(cv2.merge((l, a, b)), cv2.COLOR_LAB2BGR)