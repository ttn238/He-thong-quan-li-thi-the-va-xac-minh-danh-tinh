"""Trich dac diem nhan dang co ban tu vung anh: mau sac (trang phuc, xe)."""
import cv2
import numpy as np

_COLOR_NAMES = ["black", "white", "gray", "red", "orange", "yellow", "green", "cyan", "blue", "purple", "pink"]


def dominant_color(region_bgr: np.ndarray) -> str:
    """Ten mau chiem uu the trong vung anh (tieng Anh, on dinh cho API)."""
    if region_bgr is None or region_bgr.size == 0:
        return "unknown"
    hsv = cv2.cvtColor(region_bgr, cv2.COLOR_BGR2HSV).reshape(-1, 3).astype(np.int32)
    hue, sat, val = hsv[:, 0], hsv[:, 1], hsv[:, 2]
    idx = np.full(len(hsv), 2)  # gray
    idx[val < 55] = 0           # black
    chromatic = (val >= 55) & (sat >= 45)
    idx[(val >= 55) & (sat < 45) & (val >= 190)] = 1  # white
    h = hue[chromatic]
    names = np.select(
        [(h < 8) | (h >= 170), h < 22, h < 35, h < 85, h < 100, h < 130, h < 155],
        [3, 4, 5, 6, 7, 8, 9],
        default=10,
    )
    idx[chromatic] = names
    return _COLOR_NAMES[int(np.bincount(idx, minlength=len(_COLOR_NAMES)).argmax())]


def person_attributes(bgr: np.ndarray, box: tuple[int, int, int, int]) -> dict[str, str]:
    """Uoc luong mau ao (than tren) va quan (than duoi) tu khung nguoi."""
    x1, y1, x2, y2 = box
    h, w = y2 - y1, x2 - x1
    if h < 20 or w < 10:
        return {}
    cx1, cx2 = x1 + int(w * 0.2), x2 - int(w * 0.2)
    upper = bgr[y1 + int(h * 0.20): y1 + int(h * 0.50), cx1:cx2]
    lower = bgr[y1 + int(h * 0.55): y1 + int(h * 0.90), cx1:cx2]
    return {"upper_color": dominant_color(upper), "lower_color": dominant_color(lower)}


def vehicle_attributes(bgr: np.ndarray, box: tuple[int, int, int, int]) -> dict[str, str]:
    x1, y1, x2, y2 = box
    h, w = y2 - y1, x2 - x1
    if h < 10 or w < 10:
        return {}
    core = bgr[y1 + int(h * 0.25): y1 + int(h * 0.75), x1 + int(w * 0.2): x2 - int(w * 0.2)]
    return {"color": dominant_color(core)}
