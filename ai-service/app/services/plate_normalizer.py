"""Chuan hoa van ban OCR thanh bien so xe Viet Nam. Module thuan (khong phu thuoc mo hinh)
nen de kiem thu.

Dinh dang ho tro (ky tu khong dau):
    - O to/xe may moi: 29A-123.45, 51F-123.45, 29B1-123.45 (xe may, series 2 ky tu), 59-MD1 123.45 (series 3 ky tu)
    - Dinh dang cu 4 so: 29A-1234, 29B1-1234
    - Bien 2 dong: "29-B1" + "123.45" (hai dong duoc noi lai truoc khi chuan hoa)
"""
import re
import unicodedata
from dataclasses import dataclass
from typing import Optional

# Chu cai duoc dung lam series cua bien so Viet Nam (khong co I, J, O, Q, R, W)
VALID_SERIES_LETTERS = set("ABCDEFGHKLMNPSTUVXYZ")

# Sua loi OCR theo vi tri
_TO_DIGIT = {"O": "0", "Q": "0", "D": "0", "I": "1", "L": "1", "Z": "2", "S": "5", "G": "6", "B": "8"}
_TO_LETTER = {"0": "D", "8": "B", "5": "S", "2": "Z", "6": "G", "4": "A", "7": "T"}


@dataclass(frozen=True)
class NormalizedPlate:
    plate: Optional[str]          # vi du "29A-123.45"; None neu khong parse duoc
    compact: str                  # vi du "29A12345" (hoac chuoi sach neu khong hop le)
    province_code: Optional[str]
    series: Optional[str]
    number: Optional[str]
    corrections: int              # so ky tu da sua do nham lan OCR
    is_valid_format: bool


def _strip_accents(text: str) -> str:
    text = text.replace("Đ", "D").replace("đ", "d")  # D, d co gach ngang
    return "".join(
        c for c in unicodedata.normalize("NFD", text) if unicodedata.category(c) != "Mn"
    )


def clean_text(raw: str) -> str:
    return _strip_accents(raw or "").upper()


def _separator_boundaries(cleaned: str) -> set[int]:
    """Vi tri (chi so trong chuoi compact) ma sau do OCR doc thay khoang trang hoac gach."""
    boundaries: set[int] = set()
    idx = 0
    pending_sep = False
    for ch in cleaned:
        if ch.isalnum():
            if pending_sep and idx > 0:
                boundaries.add(idx)
            pending_sep = False
            idx += 1
        elif ch in " -_":
            pending_sep = True
    return boundaries


def _fix(ch: str, table: dict[str, str]) -> tuple[str, int]:
    fixed = table.get(ch)
    return (fixed, 1) if fixed and fixed != ch else (ch, 0)


def _try_split(compact: str, series_len: int, num_len: int):
    """Thu cat compact theo (series_len, num_len); tra (province, series, number, corrections) hoac None."""
    if 2 + series_len + num_len != len(compact):
        return None
    corrections = 0
    prov_chars = []
    for ch in compact[:2]:
        c, n = ch, 0
        if not ch.isdigit():
            c, n = _fix(ch, _TO_DIGIT)
            if not c.isdigit():
                return None
        prov_chars.append(c)
        corrections += n
    series_first = compact[2]
    if not series_first.isalpha():
        series_first, n = _fix(series_first, _TO_LETTER)
        if not series_first.isalpha():
            return None
        corrections += n
    series = series_first
    if series_len >= 2:
        series += compact[3:2 + series_len]  # cac ky tu sau chu dau (chu/so) giu nguyen
    num_chars = []
    for ch in compact[2 + series_len:]:
        c, n = ch, 0
        if not ch.isdigit():
            c, n = _fix(ch, _TO_DIGIT)
            if not c.isdigit():
                return None
        num_chars.append(c)
        corrections += n
    return "".join(prov_chars), series, "".join(num_chars), corrections


def normalize_plate(raw: str) -> NormalizedPlate:
    cleaned = clean_text(raw)
    compact = "".join(c for c in cleaned if c.isalnum())
    invalid = NormalizedPlate(None, compact, None, None, None, 0, False)
    if not 7 <= len(compact) <= 10:
        return invalid

    boundaries = _separator_boundaries(cleaned)
    best = None
    for series_len in (1, 2, 3):
        for num_len in (5, 4):
            parts = _try_split(compact, series_len, num_len)
            if parts is None:
                continue
            province, series, number, corrections = parts
            if int(province) < 11 or series[0] not in VALID_SERIES_LETTERS:
                continue
            # Diem nho hon = tot hon: it sua loi, khop vi tri tach trong van ban goc, uu tien 5 so
            score = corrections * 3 + (0 if num_len == 5 else 1)
            if (2 + series_len) in boundaries:
                score -= 2
            if best is None or score < best[0]:
                best = (score, province, series, number, corrections)
    if best is None:
        return invalid

    _, province, series, number, corrections = best
    pretty_number = f"{number[:3]}.{number[3:]}" if len(number) == 5 else number
    return NormalizedPlate(
        plate=f"{province}{series}-{pretty_number}",
        compact=f"{province}{series}{number}",
        province_code=province,
        series=series,
        number=number,
        corrections=corrections,
        is_valid_format=True,
    )
