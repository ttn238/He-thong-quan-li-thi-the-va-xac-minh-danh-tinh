"""Trich xuat thong tin giay to tuy than (CCCD/CMND/GPLX) tu cac dong OCR.

Module thuan (khong phu thuoc mo hinh/OpenCV) nen kiem thu nhanh. Cach lam:
  1. Lam sach van ban (NFC, khoang trang, dau phan cach) va sap xep dong theo (y, x).
  2. Tim "nhan" (anchor) tieng Viet co dau/khong dau/song ngu tren ban khong dau, in hoa.
  3. Gia tri = phan van ban sau nhan tren cung dong; neu rong thi lay dong ben phai cung hang
     hoac dong ngay ben duoi (theo toa do; neu engine khong tra toa do thi theo thu tu dong).
  4. So CCCD/CMND va ngay thang duoc khop bang regex, co sua loi nham O/0, I/1, B/8, S/5...

Nguyen tac: truong khong co trong van ban -> None (khong doan). Neu khong nhan dien duoc
loai giay to thi tat ca truong deu None.
"""
import re
import statistics
import unicodedata
from dataclasses import dataclass
from datetime import date
from typing import Callable, Optional, Protocol

from app.schemas.ocr_schema import DocumentExtractionResult, ExtractedField
from app.schemas.prediction_schema import BoundingBox, ImageInfo

Box = tuple[float, float, float, float]  # x1, y1, x2, y2


class LineLike(Protocol):
    """Tuong thich voi `ocr_service.OcrLine` (tranh import vong)."""

    text: str
    confidence: float
    box: Box


FIELD_NAMES = (
    "id_number",
    "full_name",
    "date_of_birth",
    "gender",
    "nationality",
    "hometown",
    "residence",
    "expiry_date",
    "issue_date",
)
_MULTILINE_FIELDS = {"hometown", "residence"}
_MAX_VALUE_LINES = 3

# ---------------------------------------------------------------- lam sach van ban

_CHAR_MAP = str.maketrans(
    {
        "\u00a0": " ", "\u200b": "", "\u200c": "", "\u200d": "", "\ufeff": "", "\t": " ",
        "\u2010": "-", "\u2011": "-", "\u2012": "-", "\u2013": "-", "\u2014": "-", "\u2212": "-",
        "\uff1a": ":", "\uff0f": "/", "\u2044": "/",
    }
)
_LEAD_JUNK = " :-.,;|/*_"


def clean_text(text: str) -> str:
    """NFC, doi \\r\\n -> \\n, gop khoang trang, bo khoang trang dau/cuoi. Khong doi chu hoa/thuong."""
    text = unicodedata.normalize("NFC", text or "").replace("\r\n", "\n").replace("\r", "\n")
    text = text.translate(_CHAR_MAP)
    return re.sub(r"[ ]{2,}", " ", text).strip()


def _fold_char(c: str) -> str:
    if c in "đĐ":
        return "D"
    base = "".join(x for x in unicodedata.normalize("NFD", c) if unicodedata.category(x) != "Mn")
    return base.upper() if len(base) == 1 else c.upper()


def fold(text: str) -> str:
    """Bo dau + in hoa, GIU NGUYEN DO DAI (tung ky tu NFC -> 1 ky tu) de chi so khop voi van ban goc."""
    return "".join(_fold_char(c) for c in text)


# ---------------------------------------------------------------- nhan (anchor)

def _alt(*patterns: str) -> str:
    return "(?:" + "|".join(patterns) + ")"


_LABEL_SRC: dict[str, list[str]] = {
    "id_number": [
        r"SO\s*/\s*NO", r"SO\s*(?:CCCD|CMND|GPLX|CAN\s*CUOC|DINH\s*DANH(?:\s*CA\s*NHAN)?|THE)",
        r"ID\s*NO", r"\bNO\s*[:.]", r"^[\s:\-.|*]*SO\s*[:.]",
    ],
    "full_name": [r"\bHO\s*(?:VA\s*)?TEN(?:\s*KHAI\s*SINH)?", r"FULL\s*NAME", r"\bNAME\b"],
    "date_of_birth": [r"NGAY[\s,]*(?:THANG[\s,]*NAM[\s,]*)?SINH", r"SINH\s*NGAY", r"DATE\s*OF\s*BIRTH", r"\bDOB\b"],
    "gender": [r"GIOI\s*TINH", r"\bSEX\b"],
    "nationality": [r"QUOC\s*TICH", r"NATIONALITY"],
    "hometown": [r"QUE\s*QUAN", r"NGUYEN\s*QUAN", r"PLACE\s*OF\s*ORIGIN", r"NOI\s*(?:DANG\s*KY\s*KHAI\s*SINH|DKKS)", r"\bDKKS\b"],
    "residence": [
        r"(?:HO\s*KHAU\s*)?(?:NOI\s*)?(?:DKHK\s*)?THUONG\s*TRU", r"PLACE\s*OF\s*RESIDENCE",
        r"NOI\s*CU\s*TRU", r"\bADDRESS\b",
    ],
    "expiry_date": [
        r"(?:CO\s*)?GIA\s*TRI\s*(?:DEN|TOI)", r"DATE\s*OF\s*EXPIRY", r"\bEXPIR(?:Y|ES)\b",
        r"VALID\s*(?:UNTIL|THROUGH|TO)", r"HET\s*HAN",
    ],
    "issue_date": [
        r"NGAY\s*CAP", r"DATE\s*OF\s*ISSUE", r"NGAY[\s,]*THANG[\s,]*NAM(?![\s,]*SINH)", r"DATE[\s,]*MONTH[\s,]*YEAR",
    ],
}
# Nhan khong thuoc truong nao: chi dung de CAT gia tri nhieu dong, khong lay du lieu
_STOP_SRC = [
    r"DAN\s*TOC", r"TON\s*GIAO", r"ETHNIC\w*", r"RELIGION", r"DAC\s*DIEM\s*NHAN\s*DANG", r"NOI\s*CAP",
    r"NOI\s*SINH", r"PLACE\s*OF\s*BIRTH", r"CUC\s*TRUONG", r"CONG\s*HOA", r"SOCIALIST", r"DOC\s*LAP",
    r"CAN\s*CUOC", r"CHUNG\s*MINH", r"GIAY\s*PHEP", r"\bCLASS\b", r"HANG\s*/\s*CLASS",
]


def _compile_label(sources: list[str]) -> "re.Pattern[str]":
    one = _alt(*sources)
    return re.compile(one + r"(?:[\s/:\-.,|]*" + one + r")*")


_LABEL_RE = {f: _compile_label(src) for f, src in _LABEL_SRC.items()}
_STOP_RE = _compile_label(_STOP_SRC)


@dataclass
class _Hit:
    field: Optional[str]  # None = nhan chan (stopper)
    start: int
    end: int


def _find_hits(folded: str) -> list[_Hit]:
    hits = [_Hit(f, m.start(), m.end()) for f, rx in _LABEL_RE.items() for m in rx.finditer(folded)]
    hits += [_Hit(None, m.start(), m.end()) for m in _STOP_RE.finditer(folded)]
    hits.sort(key=lambda h: (h.start, -(h.end - h.start)))
    kept: list[_Hit] = []
    for h in hits:  # bo nhan chong lan (giu nhan dai/som hon)
        if not kept or h.start >= kept[-1].end:
            kept.append(h)
    return kept


# ---------------------------------------------------------------- dong OCR

@dataclass(eq=False)
class _Line:
    text: str          # da lam sach
    fold: str
    conf: float
    box: Box
    hits: list[_Hit]

    @property
    def geo(self) -> bool:
        return self.box[2] > self.box[0] and self.box[3] > self.box[1]

    @property
    def cy(self) -> float:
        return (self.box[1] + self.box[3]) / 2

    @property
    def h(self) -> float:
        return self.box[3] - self.box[1]

    @property
    def starts_with_label(self) -> bool:
        if not self.hits:
            return False
        lead = len(self.fold) - len(self.fold.lstrip(_LEAD_JUNK))
        return self.hits[0].start <= lead


def _prepare_lines(lines: list[LineLike]) -> list[_Line]:
    out: list[_Line] = []
    for l in lines:
        for part in clean_text(l.text).split("\n"):
            part = part.strip()
            if part:
                f = fold(part)
                out.append(_Line(part, f, float(l.confidence), tuple(float(v) for v in l.box), _find_hits(f)))
    return out


def _reading_order(lines: list[_Line]) -> list[_Line]:
    """Tren xuong, trai qua phai: gom thanh hang theo tam y roi sap theo x. Dong khong co toa do
    giu nguyen thu tu dau vao (sap xep on dinh)."""
    geo = [l for l in lines if l.geo]
    if len(geo) < len(lines):
        return list(lines)
    tol = 0.5 * (statistics.median(l.h for l in geo) if geo else 0)
    rows: list[list[_Line]] = []
    for l in sorted(lines, key=lambda l: l.cy):
        if rows and abs(l.cy - statistics.fmean(r.cy for r in rows[-1])) <= tol:
            rows[-1].append(l)
        else:
            rows.append([l])
    return [l for row in rows for l in sorted(row, key=lambda l: l.box[0])]


# ---------------------------------------------------------------- chuan hoa gia tri

_OCR_DIGIT = {
    "O": "0", "o": "0", "Q": "0", "D": "0", "I": "1", "l": "1", "|": "1", "i": "1",
    "B": "8", "S": "5", "s": "5", "Z": "2",
}
_DIGIT_CLASS = r"[0-9OoQDIl|iBSsZ]"
_ID_TOKEN = re.compile(rf"(?<![0-9A-Za-z]){_DIGIT_CLASS}(?:[ .\-]?{_DIGIT_CLASS}){{8,13}}(?![0-9A-Za-z])")
_DATE_NUM = re.compile(
    rf"(?<![0-9A-Za-z])({_DIGIT_CLASS}{{1,2}})\s*[/\-.]\s*({_DIGIT_CLASS}{{1,2}})\s*[/\-.]\s*({_DIGIT_CLASS}{{4}})(?![0-9A-Za-z])"
)
_DATE_WORDS = re.compile(r"NGAY\s*(\d{1,2})\s*THANG\s*(\d{1,2})\s*NAM\s*(\d{4})")


def _to_digits(s: str) -> tuple[str, int]:
    """Doi ky tu hay bi OCR nham sang so. Tra (chuoi, so ky tu da sua)."""
    out, fixes = [], 0
    for c in s:
        if c.isdigit():
            out.append(c)
        elif c in _OCR_DIGIT:
            out.append(_OCR_DIGIT[c])
            fixes += 1
    return "".join(out), fixes


def _penalty(conf: float, fixes: int) -> float:
    return conf * (0.9 ** fixes)


def find_id_numbers(text: str) -> list[tuple[str, str, int]]:
    """Tim so CCCD (12) / CMND (9) trong 1 dong. Tra [(so, raw, so_ky_tu_da_sua)]."""
    if "<<" in text:  # dong MRZ mat sau the
        return []
    found = []
    for m in _ID_TOKEN.finditer(text):
        raw = m.group(0)
        compact = re.sub(r"[ .\-]", "", raw)
        real_digits = sum(c.isdigit() for c in compact)
        if real_digits < 4 or real_digits * 2 < len(compact):
            continue  # qua nhieu chu -> khong phai so
        digits, fixes = _to_digits(compact)
        if len(digits) in (9, 12):
            found.append((digits, raw, fixes))
    return found


def parse_date(text: str) -> Optional[tuple[str, str, int]]:
    """Tim ngay hop le trong `text`. Tra (DD/MM/YYYY, raw, so_ky_tu_da_sua) hoac None."""
    for m in _DATE_NUM.finditer(text):
        parts, fixes = [], 0
        for g in m.groups():
            d, f = _to_digits(g)
            parts.append(d)
            fixes += f
        d_, mo, y = (int(p) for p in parts)
        if _valid_date(d_, mo, y):
            return f"{d_:02d}/{mo:02d}/{y:04d}", m.group(0), fixes
    m = _DATE_WORDS.search(fold(text))
    if m:
        d_, mo, y = (int(g) for g in m.groups())
        if _valid_date(d_, mo, y):
            return f"{d_:02d}/{mo:02d}/{y:04d}", text[m.start():m.end()], 0
    return None


def _valid_date(d: int, m: int, y: int) -> bool:
    if not 1900 <= y <= 2100:
        return False
    try:
        date(y, m, d)
    except ValueError:
        return False
    return True


def _trim(s: str) -> str:
    return re.sub(r"\s+", " ", s.strip(_LEAD_JUNK)).strip()


def _lead(s: str) -> str:
    """Bo nhieu dau/khoang trang o DAU (giu dau ',' cuoi dong de noi dia chi nhieu dong)."""
    return re.sub(r"\s+", " ", s.strip().lstrip(_LEAD_JUNK)).strip()


def _norm_name(s: str) -> Optional[tuple[str, int]]:
    s = _trim(s)
    fixes = 0
    words = []
    for w in s.split():
        if sum(c.isalpha() for c in w) >= 2:  # sua 0->O, 1->I chi trong tu co chu
            for a, b in (("0", "O"), ("1", "I")):
                fixes += w.count(a)
                w = w.replace(a, b)
        words.append(w)
    s = re.sub(r"[^\w\s'.\-]|[\d_]", "", " ".join(words)).strip(" .-'")
    s = re.sub(r"\s+", " ", s).upper()
    return (s, fixes) if sum(c.isalpha() for c in s) >= 2 else None


def _norm_gender(s: str) -> Optional[tuple[str, int]]:
    f = fold(_trim(s))
    if re.match(r"(?:NAM|MALE|M)\b", f):
        return "Nam", 0
    if re.match(r"(?:NU|FEMALE|F)\b", f):
        return "Nữ", 0
    return None


def _norm_nationality(s: str) -> Optional[tuple[str, int]]:
    s = _trim(s)
    if re.match(r"VIET\s*NAM\b", fold(s)):
        return "Việt Nam", 0
    return (s, 0) if sum(c.isalpha() for c in s) >= 3 else None


def _norm_text(s: str) -> Optional[tuple[str, int]]:
    s = _trim(s)
    return (s, 0) if sum(c.isalnum() for c in s) >= 3 else None


def _norm_date(s: str) -> Optional[tuple[str, int]]:
    p = parse_date(s)
    return (p[0], p[2]) if p else None


def _norm_expiry(s: str) -> Optional[tuple[str, int]]:
    if re.search(r"KHONG\s*THOI\s*HAN", fold(s)):
        return "Không thời hạn", 0
    return _norm_date(s)


_NORMALIZERS: dict[str, Callable[[str], Optional[tuple[str, int]]]] = {
    "full_name": _norm_name,
    "date_of_birth": _norm_date,
    "gender": _norm_gender,
    "nationality": _norm_nationality,
    "hometown": _norm_text,
    "residence": _norm_text,
    "expiry_date": _norm_expiry,
    "issue_date": _norm_date,
}


# ---------------------------------------------------------------- ghep nhan - gia tri

def _union(lines: list[_Line]) -> Box:
    return (
        min(l.box[0] for l in lines), min(l.box[1] for l in lines),
        max(l.box[2] for l in lines), max(l.box[3] for l in lines),
    )


def _prefix(line: _Line) -> str:
    """Phan van ban truoc nhan dau tien (gia tri 'chay' sang dong ke tiep)."""
    return line.text[: line.hits[0].start] if line.hits else line.text


def _value_below(ref: _Line, lines: list[_Line], idx: int, first: bool) -> Optional[_Line]:
    """Dong tiep theo chua gia tri cua `ref`. Co toa do: ben phai cung hang, hoac ngay ben duoi
    canh le trai; khong co toa do: dong ke tiep theo thu tu doc. None neu dong do la nhan khac."""
    if not ref.geo:
        cand = lines[idx + 1] if idx + 1 < len(lines) else None
    else:
        best, best_rank = None, None
        for j, m in enumerate(lines):
            if m is ref or not m.geo:
                continue
            same_row = first and abs(m.cy - ref.cy) <= 0.6 * max(ref.h, m.h) and m.box[0] >= ref.box[2] - 0.25 * (ref.box[2] - ref.box[0])
            gap = m.box[1] - ref.box[3]
            below = (
                m.cy > ref.cy + 0.5 * ref.h
                and gap <= 2.0 * ref.h
                and m.box[0] <= ref.box[2]
                and m.box[2] >= ref.box[0]
                and m.box[0] >= ref.box[0] - 2.0 * ref.h
            )
            if not (same_row or below):
                continue
            rank = (0, m.box[0]) if same_row else (1, m.cy)
            if best_rank is None or rank < best_rank:
                best, best_rank = m, rank
        cand = best
    if cand is None or cand.starts_with_label:
        return None
    return cand


@dataclass
class _Found:
    value: str
    raw: str
    conf: float
    box: Box
    fixes: int = 0


def _collect(lines: list[_Line], idx: int, hit_no: int, field: str) -> list[tuple[str, _Line]]:
    """Cac (doan van ban, dong) lam nen gia tri cho nhan `hit_no` cua dong `idx`."""
    line = lines[idx]
    h = line.hits[hit_no]
    nxt = line.hits[hit_no + 1].start if hit_no + 1 < len(line.hits) else len(line.text)
    inline = _lead(line.text[h.end:nxt])
    parts: list[tuple[str, _Line]] = []
    last = line
    if inline:
        parts.append((inline, line))
        if hit_no + 1 < len(line.hits):  # sau do con nhan khac tren cung dong
            return parts
    else:
        v = _value_below(line, lines, idx, first=True)
        if v is None:
            return []
        pre = _lead(_prefix(v))
        if not pre:
            return []
        parts.append((pre, v))
        last = v
        if v.hits:
            return parts
    if field in _MULTILINE_FIELDS:
        while len(parts) < _MAX_VALUE_LINES:
            n = _value_below(last, lines, lines.index(last), first=False)
            if n is None or n.hits:
                break
            parts.append((_lead(n.text), n))
            last = n
    return parts


def _extract_labelled(lines: list[_Line]) -> dict[str, _Found]:
    found: dict[str, _Found] = {}
    for idx, line in enumerate(lines):
        for k, h in enumerate(line.hits):
            f = h.field
            if f is None or f == "id_number" or f in found:
                continue
            parts = _collect(lines, idx, k, f)
            if not parts:
                continue
            raw = " ".join(p for p, _ in parts)
            norm = _NORMALIZERS[f](raw)
            if norm is None:
                continue
            value, fixes = norm
            src = [l for _, l in parts]
            conf = sum(l.conf for l in src) / len(src)
            found[f] = _Found(value, raw, conf, _union(src), fixes)
    return found


def _extract_id(lines: list[_Line]) -> Optional[_Found]:
    cands: list[tuple[tuple, _Found]] = []
    for idx, line in enumerate(lines):
        id_hit = any(h.field == "id_number" for h in line.hits)
        prev_anchor = idx > 0 and bool(lines[idx - 1].hits) and lines[idx - 1].hits[-1].field == "id_number" \
            and lines[idx - 1].hits[-1].end >= len(lines[idx - 1].text) - 1
        for digits, raw, fixes in find_id_numbers(line.text):
            anchored = id_hit or prev_anchor
            rank = (not anchored, len(digits) != 12, idx)
            cands.append((rank, _Found(digits, raw, line.conf, line.box, fixes)))
    return min(cands, key=lambda c: c[0])[1] if cands else None


def _extract_issue_fallback(lines: list[_Line]) -> Optional[_Found]:
    """Mat sau CMND hay ghi 'Ngay 15 thang 03 nam 2010' khong co nhan 'Ngay cap'."""
    for l in lines:
        m = _DATE_WORDS.search(l.fold)
        if not m:
            continue
        d, mo, y = (int(g) for g in m.groups())
        if _valid_date(d, mo, y):
            return _Found(f"{d:02d}/{mo:02d}/{y:04d}", l.text[m.start():m.end()], l.conf, l.box)
    return None


# ---------------------------------------------------------------- loai giay to

def detect_document_type(folded_text: str) -> str:
    """Theo tu khoa tieu de; 'unknown' neu khong thay."""
    if re.search(r"GIAY\s*PHEP\s*LAI\s*XE|DRIVER'?S?\s*LICEN[SC]E", folded_text):
        return "driver_license"
    if re.search(r"CAN\s*CUOC|CITIZEN\s*IDENTIT", folded_text):
        return "cccd"
    if re.search(r"CHUNG\s*MINH\s*NHAN\s*DAN", folded_text):
        return "cmnd"
    return "unknown"


def _default_to_box(b: Box) -> BoundingBox:
    return BoundingBox(x_min=int(b[0]), y_min=int(b[1]), x_max=int(b[2]), y_max=int(b[3]))


def extract_document(
    lines: list[LineLike],
    image_info: ImageInfo,
    min_confidence: float = 0.5,
    to_box: Callable[[Box], BoundingBox] = _default_to_box,
) -> DocumentExtractionResult:
    """Phan tich cac dong OCR thanh `DocumentExtractionResult`.

    `to_box` doi toa do cua engine sang toa do anh goc (mac dinh: giu nguyen).
    Dong co confidence < `min_confidence` bi bo qua khi trich xuat nhung van nam trong `raw_texts`.
    """
    all_lines = _reading_order(_prepare_lines(lines))
    raw_texts = [l.text for l in all_lines]
    use = _reading_order([l for l in all_lines if l.conf >= min_confidence])

    found = _extract_labelled(use)
    id_found = _extract_id(use)
    if id_found:
        found["id_number"] = id_found
    if "issue_date" not in found and (fb := _extract_issue_fallback(use)):
        found["issue_date"] = fb

    doc_type = detect_document_type(" ".join(l.fold for l in use))
    if doc_type == "unknown" and len(found) >= 2 and id_found:
        # Khong co tieu de nhung co so + them truong dinh danh: suy ra theo do dai so
        doc_type = "cccd" if len(id_found.value) == 12 else "cmnd"
    if doc_type == "unknown":
        found = {}  # khong phai giay to tuy than -> khong tra ve du lieu doan

    fields: dict[str, Optional[ExtractedField]] = {}
    for name in FIELD_NAMES:
        f = found.get(name)
        box = None
        if f and f.box[2] > f.box[0] and f.box[3] > f.box[1]:
            box = to_box(f.box)
        fields[name] = (
            ExtractedField(
                value=f.value, raw_value=f.raw, confidence=round(min(max(_penalty(f.conf, f.fixes), 0.0), 1.0), 4), box=box
            )
            if f
            else None
        )
    present = [f for f in fields.values() if f]
    overall = round(sum(f.confidence for f in present) / len(present), 4) if present else 0.0
    return DocumentExtractionResult(
        image=image_info,
        document_type=doc_type,
        is_valid_document=doc_type != "unknown" and fields["id_number"] is not None,
        overall_confidence=overall,
        missing_fields=[n for n in FIELD_NAMES if fields[n] is None],
        raw_texts=raw_texts,
        **fields,
    )
