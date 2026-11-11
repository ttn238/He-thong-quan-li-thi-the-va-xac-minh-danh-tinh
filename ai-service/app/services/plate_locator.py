"""Dinh vi vung nghi la bien so bang OpenCV (canh + hinh chu nhat). Tra ve cac o ung vien
(x1, y1, x2, y2) tren anh dau vao, sap xep theo do uu tien. OCR se chay tren cac vung nay
truoc, sau do moi tu quay ve toan anh."""
import cv2
import numpy as np

Box = tuple[int, int, int, int]


def locate_plate_regions(bgr: np.ndarray, max_regions: int = 3) -> list[Box]:
    h, w = bgr.shape[:2]
    img_area = float(h * w)
    gray = cv2.cvtColor(bgr, cv2.COLOR_BGR2GRAY)
    gray = cv2.bilateralFilter(gray, 9, 40, 40)
    edges = cv2.Canny(gray, 60, 180)
    edges = cv2.morphologyEx(edges, cv2.MORPH_CLOSE, cv2.getStructuringElement(cv2.MORPH_RECT, (9, 3)))
    contours, _ = cv2.findContours(edges, cv2.RETR_LIST, cv2.CHAIN_APPROX_SIMPLE)

    scored: list[tuple[float, Box]] = []
    for c in contours:
        area = cv2.contourArea(c)
        if area < img_area * 0.003 or area > img_area * 0.6:
            continue
        x, y, bw, bh = cv2.boundingRect(c)
        if bh == 0:
            continue
        aspect = bw / bh
        # Bien 1 dong ~ 4:1 .. 5:1, bien 2 dong ~ 1.2:1 .. 1.5:1
        one_line = 2.5 <= aspect <= 6.5
        two_line = 1.1 <= aspect <= 1.7
        if not (one_line or two_line):
            continue
        fill = area / float(bw * bh)  # mat do lap day hinh chu nhat
        if fill < 0.5:
            continue
        pad_x, pad_y = int(bw * 0.06), int(bh * 0.1)
        box = (max(0, x - pad_x), max(0, y - pad_y), min(w, x + bw + pad_x), min(h, y + bh + pad_y))
        scored.append((fill * (area / img_area) ** 0.25, box))

    scored.sort(key=lambda t: t[0], reverse=True)
    picked: list[Box] = []
    for _, box in scored:
        if all(_iou(box, p) < 0.5 for p in picked):
            picked.append(box)
        if len(picked) >= max_regions:
            break
    return picked


def _iou(a: Box, b: Box) -> float:
    ix1, iy1, ix2, iy2 = max(a[0], b[0]), max(a[1], b[1]), min(a[2], b[2]), min(a[3], b[3])
    inter = max(0, ix2 - ix1) * max(0, iy2 - iy1)
    if inter == 0:
        return 0.0
    union = (a[2] - a[0]) * (a[3] - a[1]) + (b[2] - b[0]) * (b[3] - b[1]) - inter
    return inter / union
