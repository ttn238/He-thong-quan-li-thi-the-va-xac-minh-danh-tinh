"""Chuan hoa chuoi dia chi Viet Nam (tu OCR hoac nhap tay) truoc khi tra cuu.

Chi dung thu vien chuan: khong goi mang, khong phu thuoc FastAPI nen test duoc ngay.

Day la chuan hoa theo QUY TAC (heuristic), khong phai bo phan tich dia chi day du. Muc tieu la
giam nhieu cho nguon tra cuu: khoang trang/dau cau, viet tat pho bien (P., Q., TP., TPHCM...),
dau phay giua cac cap hanh chinh. Moi quy tac nam o hang so/ham rieng de de sua va de test.
"""
import re
import unicodedata
from dataclasses import dataclass
from typing import Iterable

MAX_RAW_LENGTH = 1000  # chan chuoi qua dai (OCR loi) truoc khi chay regex

_INVISIBLE = dict.fromkeys(map(ord, "​‌‍⁠﻿­"), None)
_NOISE_CHARS = re.compile(r"[|_~*^•·●■□<>\[\]{}\\\"“”‘’`]")
_PAREN_CONTENT = re.compile(r"\([^)]*\)")
_LABEL_PREFIX = re.compile(
    r"^\s*(?:địa\s*chỉ|dia\s*chi|đ\s*/\s*c|đc|add|address|nơi\s*ở|nơi\s*cư\s*trú|nơi\s*thường\s*trú|"
    r"thường\s*trú|nơi\s*phát\s*hiện|nơi\s*mất\s*tích)\s*[:：]\s*",
    re.IGNORECASE,
)

# Tu viet tat/khong dau -> dang day du. THU TU QUAN TRONG: quy tac dac thu dat truoc quy tac chung.
_ABBREVIATIONS: list[tuple[re.Pattern, str]] = [
    (re.compile(r"(?<!\w)(?:tp\.?\s*)?(?:hcmc?|ho\s+chi\s+minh\s+city)(?!\w)", re.I), "Thành phố Hồ Chí Minh"),
    (re.compile(r"(?<!\w)(?:tp\.?\s*)?(?:hn|ha\s+noi)(?!\w)", re.I), "Hà Nội"),
    (re.compile(r"(?<!\w)t\.?p(?:\.\s*|\s+)(?=\w)", re.I), "Thành phố "),
    (re.compile(r"(?<!\w)tx(?:\.\s*|\s+)(?=\w)", re.I), "Thị xã "),
    # Cac viet tat de nham voi ten rieng/so phong: chi mo rong khi co dau cham va theo sau la chu
    (re.compile(r"(?<!\w)tt\.\s*(?=[^\W\d_])", re.I), "Thị trấn "),
    (re.compile(r"(?<!\w)h\.\s*(?=[^\W\d_])", re.I), "Huyện "),
    (re.compile(r"(?<!\w)x\.\s*(?=[^\W\d_])", re.I), "Xã "),
    (re.compile(r"(?<!\w)kp(?:\.\s*|\s*(?=\d)|\s+(?=\w))", re.I), "Khu phố "),
    (re.compile(r"(?<!\w)p(?:\.\s*(?=\w)|\s*(?=\d))", re.I), "Phường "),
    (re.compile(r"(?<!\w)q(?:\.\s*(?=\w)|\s*(?=\d))", re.I), "Quận "),
    (re.compile(r"(?<!\w)ql\.?\s*(?=\d)", re.I), "Quốc lộ "),
    # Khong dau (thuong gap khi OCR/nhap nhanh)
    (re.compile(r"(?<!\w)phuong\s*(?=\d)", re.I), "Phường "),
    (re.compile(r"(?<!\w)quan\s*(?=\d)", re.I), "Quận "),
    (re.compile(r"(?<!\w)thanh\s+pho(?!\w)", re.I), "Thành phố"),
    (re.compile(r"(?<!\w)thi\s+xa(?!\w)", re.I), "Thị xã"),
    (re.compile(r"(?<!\w)thi\s+tran(?!\w)", re.I), "Thị trấn"),
    (re.compile(r"(?<!\w)khu\s+pho(?!\w)", re.I), "Khu phố"),
    (re.compile(r"(?<!\w)huyen(?!\w)", re.I), "Huyện"),
    (re.compile(r"(?<!\w)ho\s+chi\s+minh(?!\w)", re.I), "Hồ Chí Minh"),
]

# Dang chuan cua cac tu chi cap hanh chinh (de nhan dien ranh gioi giua cac phan)
_ADMIN_WORDS = re.compile(r"(?<!\w)(thành\s+phố|thị\s+xã|thị\s+trấn|khu\s+phố|phường|quận|huyện)(?!\w)", re.I)
_ADMIN_CANONICAL = {
    "thành phố": "Thành phố",
    "thị xã": "Thị xã",
    "thị trấn": "Thị trấn",
    "khu phố": "Khu phố",
    "phường": "Phường",
    "quận": "Quận",
    "huyện": "Huyện",
}
# Chen dau phay truoc tu chi cap hanh chinh. Cố ý KHONG gom "Xã"/"Tỉnh" vi de nham voi ten duong
# ("Xã Đàn", "Tỉnh lộ 10").
_ADMIN_BOUNDARY = re.compile(
    r"(?<!,)\s+(?=(?:Phường|Quận|Huyện|Thị xã|Thị trấn|Thành phố|Khu phố)(?!\w))"
)
_ADMIN_DUPLICATE = re.compile(r"(Thành phố|Thị xã|Thị trấn|Quận|Huyện|Phường|Khu phố)(?:\s+\1)+")
# "Số 12 ..." / "No. 12 ..." o dau mot phan -> "12 ..." (khong dong cham "Đường Số 7")
_HOUSE_NUMBER_WORD = re.compile(r"(^|,\s*)(?:số\s*nhà|so\s*nha|số|so|no\.?|nº)\s*(?=\d)", re.I)
_COUNTRY_FOLDED = {"viet nam", "vietnam", "vn"}
_LEGACY_LEVEL = re.compile(r"^(?:Quận|Huyện|Thị xã)(?!\w)")


def fold(text: str) -> str:
    """Bo dau, ha chu thuong, thay ky tu khong phai chu/so bang 1 khoang trang. Dung lam khoa so khop/cache."""
    text = text.replace("đ", "d").replace("Đ", "D")
    decomposed = unicodedata.normalize("NFD", text)
    no_marks = "".join(c for c in decomposed if unicodedata.category(c) != "Mn")
    return re.sub(r"[^0-9a-z]+", " ", no_marks.lower()).strip()


@dataclass(frozen=True)
class FallbackQuery:
    query: str
    dropped: tuple[str, ...]  # cac phan da bo (tu phan cu the nhat), rong neu la chuoi day du


@dataclass(frozen=True)
class NormalizedAddress:
    raw: str
    text: str
    parts: tuple[str, ...]
    notes: tuple[str, ...] = ()

    @property
    def is_empty(self) -> bool:
        return not self.text

    @property
    def folded(self) -> str:
        return fold(self.text)

    def fallback_queries(self, max_attempts: int = 3) -> list[FallbackQuery]:
        """Chuoi day du, roi bo dan phan cu the nhat (so nha, duong...). Luon giu it nhat 2 phan
        (tru khi chi co 1 phan) de khong bien thanh truy van qua rong nhu chi con ten thanh pho."""
        parts = list(self.parts)
        if not parts:
            return []
        out: list[FallbackQuery] = []
        for i in range(max(len(parts) - 1, 1)):
            if len(out) >= max_attempts:
                break
            out.append(FallbackQuery(", ".join(parts[i:]), tuple(parts[:i])))
        return out


def normalize_address(raw: object) -> NormalizedAddress:
    original = "" if raw is None else str(raw)[:MAX_RAW_LENGTH]
    notes: list[str] = []

    s = unicodedata.normalize("NFC", original).translate(_INVISIBLE).replace(" ", " ")
    s = re.sub(r"[\r\n\t]+", ", ", s)  # xuong dong (OCR nhieu dong) -> dau phay
    if _PAREN_CONTENT.search(s):
        notes.append("Đã bỏ phần ghi chú trong ngoặc đơn.")
        s = _PAREN_CONTENT.sub(" ", s)
    s = re.sub(r"[()]", " ", s)
    s = _NOISE_CHARS.sub(" ", s)
    s = _LABEL_PREFIX.sub("", s, count=1)
    s = re.sub(r"\s*;\s*", ", ", s)
    s = re.sub(r"\s+[-–—]\s+", ", ", s)  # gach ngang co khoang trang = ngan cach (khong dung "12-14")
    s = _fix_case(s)

    for pattern, replacement in _ABBREVIATIONS:
        s = pattern.sub(replacement, s)
    s = _ADMIN_WORDS.sub(lambda m: _ADMIN_CANONICAL[re.sub(r"\s+", " ", m.group(1).lower())], s)
    s = _ADMIN_DUPLICATE.sub(r"\1", s)
    s = _HOUSE_NUMBER_WORD.sub(r"\1", s)
    s = _ADMIN_BOUNDARY.sub(", ", s)

    parts = _split_parts(s)
    if any(_LEGACY_LEVEL.match(p) for p in parts):
        notes.append(
            "Địa chỉ có cấp quận/huyện: đơn vị hành chính có thể đã thay đổi nên có thể không khớp dữ liệu bản đồ."
        )
    return NormalizedAddress(raw=original.strip(), text=", ".join(parts), parts=tuple(parts), notes=tuple(notes))


def _fix_case(s: str) -> str:
    """Chuoi toan HOA hoac toan thuong (hay gap o OCR) -> Viet Hoa Chu Cai Dau; chuoi hon hop giu nguyen."""
    letters = [c for c in s if c.isalpha()]
    if letters and (all(c.isupper() for c in letters) or all(c.islower() for c in letters)):
        return s.title()
    return s


def _split_parts(s: str) -> list[str]:
    parts: list[str] = []
    for piece in re.sub(r"\s*,\s*", ",", s).split(","):
        piece = re.sub(r"\s+", " ", piece).strip(" .-–—:/")
        if not re.search(r"\w", piece):
            continue
        if parts and fold(parts[-1]) == fold(piece):
            continue
        parts.append(piece)
    while parts and fold(parts[-1]) in _COUNTRY_FOLDED:
        parts.pop()
    return parts


def address_text_from_ocr_lines(lines: Iterable[object], min_confidence: float = 0.5) -> str:
    """Ghep cac dong OCR thanh mot chuoi dia chi (cac dong cach nhau bang dau phay).

    `lines`: doi tuong co `.text`, `.confidence` va (tuy chon) `.box = (x1, y1, x2, y2)`, vi du OcrLine
    trong ocr_service.py. Dong co do tin cay thap bi bo. Co `.box` thi sap xep tu tren xuong duoi, trai sang
    phai; khong co thi giu nguyen thu tu dau vao.
    """
    usable = [
        l for l in lines if float(getattr(l, "confidence", 0.0)) >= min_confidence and str(getattr(l, "text", "")).strip()
    ]
    if usable and all(getattr(l, "box", None) is not None for l in usable):
        usable.sort(key=lambda l: ((l.box[1] + l.box[3]) / 2, l.box[0]))
    return ", ".join(str(l.text).strip() for l in usable)
