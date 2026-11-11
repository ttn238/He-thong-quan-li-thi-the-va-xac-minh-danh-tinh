"""Kiem thu trich xuat thong tin giay to tuy than. Dung dong OCR gia lap (khong tai mo hinh).

Cac ham `*_lines()` o dau file la fixtures dung lai duoc: moi ham tra ve list[OcrLine]."""
import unicodedata

import numpy as np
import pytest

from app.schemas.prediction_schema import ImageInfo
from app.services.document_extractor import (
    FIELD_NAMES,
    clean_text,
    extract_document,
    find_id_numbers,
    parse_date,
)
from app.services.ocr_service import OcrLine, extract_document_info
from app.utils.image_processing import LoadedImage

INFO = ImageInfo(width=1000, height=640)
LINE_H = 30


def layout(rows, conf=0.95, x=200, y0=100, gap=44):
    """Moi phan tu cua `rows` la 1 dong chu (str) hoac (str, conf). Xep tu tren xuong, canh trai."""
    out = []
    for i, r in enumerate(rows):
        text, c = (r, conf) if isinstance(r, str) else r
        y = y0 + i * gap
        out.append(OcrLine(text, c, (x, y, x + 12 * len(text), y + LINE_H)))
    return out


def cccd_full_lines():
    return layout([
        "CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM",
        "Độc lập - Tự do - Hạnh phúc",
        "CĂN CƯỚC CÔNG DÂN",
        "Số / No.: 001099012345",
        "Họ và tên / Full name:",
        "NGUYỄN VĂN AN",
        "Ngày sinh / Date of birth: 01/05/1999",
        "Giới tính / Sex: Nam Quốc tịch / Nationality: Việt Nam",
        "Quê quán / Place of origin:",
        "Xã Tiên Phong, Huyện Phú Xuyên,",
        "Thành phố Hà Nội",
        "Nơi thường trú / Place of residence:",
        "Số 12 Hàng Bài, Phường Hàng Bài,",
        "Quận Hoàn Kiếm, Thành phố Hà Nội",
        "Có giá trị đến: 01/05/2039",
    ])


def cmnd_lines():
    return layout([
        "CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM",
        "GIẤY CHỨNG MINH NHÂN DÂN",
        "Số: 012345678",
        "Họ tên: TRẦN THỊ BÍCH",
        "Sinh ngày: 15-03-1975",
        "Nguyên quán: Xã Đông Hưng, Huyện Đông Hưng, Thái Bình",
        "Nơi ĐKHK thường trú:",
        "Số 5 Ngõ 10 Phố Lê Lợi,",
        "Quận Hà Đông, Hà Nội",
        "Dân tộc: Kinh",
    ])


def torn_lines():
    """Giay to rach: chi con ho ten va so, mat tieu de/ngay sinh/dia chi."""
    return layout(["Họ và tên: LÊ MINH TUẤN", "Số: 079201001234"], conf=0.7)


def noisy_lines():
    return layout([
        "CAN CUOC CONG DAN",
        "So:O0I2O5OO1234",
        "Ho va ten:NGUYEN VAN C",
        "Ngay sinh:01/O5/199O",
        "Gioi tinh:Nu",
        "Que quan-Ha Noi",
    ], conf=0.9)


def non_id_lines():
    return layout([
        "HÓA ĐƠN BÁN HÀNG",
        "Ngày 05/11/2026",
        "Mã giao dịch: 123456789012",
        "Tổng cộng: 120.000 đ",
    ])


def values(res):
    return {n: (getattr(res, n).value if getattr(res, n) else None) for n in FIELD_NAMES}


# ------------------------------------------------------------------ 1. CCCD day du

def test_cccd_full():
    res = extract_document(cccd_full_lines(), INFO)
    assert res.document_type == "cccd" and res.is_valid_document
    assert values(res) == {
        "id_number": "001099012345",
        "full_name": "NGUYỄN VĂN AN",
        "date_of_birth": "01/05/1999",
        "gender": "Nam",
        "nationality": "Việt Nam",
        "hometown": "Xã Tiên Phong, Huyện Phú Xuyên, Thành phố Hà Nội",
        "residence": "Số 12 Hàng Bài, Phường Hàng Bài, Quận Hoàn Kiếm, Thành phố Hà Nội",
        "expiry_date": "01/05/2039",
        "issue_date": None,
    }
    assert res.missing_fields == ["issue_date"]
    assert 0.9 <= res.overall_confidence <= 1.0
    assert res.full_name.box is not None and res.full_name.box.y_min < res.full_name.box.y_max
    assert res.raw_texts[0] == "CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM"


def test_cccd_labels_without_geometry_use_line_order():
    lines = [OcrLine(l.text, l.confidence, (0, 0, 0, 0)) for l in cccd_full_lines()]
    res = extract_document(lines, INFO)
    assert res.full_name.value == "NGUYỄN VĂN AN"
    assert res.gender.value == "Nam" and res.nationality.value == "Việt Nam"
    assert res.hometown.value == "Xã Tiên Phong, Huyện Phú Xuyên, Thành phố Hà Nội"
    assert res.full_name.box is None  # khong co toa do thi khong bia box


def test_value_to_the_right_on_same_row():
    lines = [
        OcrLine("Họ và tên:", 0.95, (100, 100, 220, 130)),
        OcrLine("PHẠM QUỐC DŨNG", 0.95, (240, 102, 480, 132)),
        OcrLine("Giới tính:", 0.95, (100, 150, 220, 180)),
        OcrLine("Nam", 0.95, (240, 150, 300, 180)),
        OcrLine("CĂN CƯỚC", 0.95, (100, 20, 300, 50)),
    ]
    res = extract_document(lines, INFO)
    assert res.full_name.value == "PHẠM QUỐC DŨNG" and res.gender.value == "Nam"


# ------------------------------------------------------------------ 2. CMND 9 so

def test_cmnd_9_digits():
    res = extract_document(cmnd_lines(), INFO)
    assert res.document_type == "cmnd" and res.is_valid_document
    v = values(res)
    assert v["id_number"] == "012345678"
    assert v["full_name"] == "TRẦN THỊ BÍCH"
    assert v["date_of_birth"] == "15/03/1975"
    assert v["hometown"] == "Xã Đông Hưng, Huyện Đông Hưng, Thái Bình"
    assert v["residence"] == "Số 5 Ngõ 10 Phố Lê Lợi, Quận Hà Đông, Hà Nội"
    assert v["gender"] is None and v["nationality"] is None
    assert "Kinh" not in v["residence"]  # nhan 'Dan toc' chan gia tri nhieu dong


# ------------------------------------------------------------------ 3. rach/mo/thieu

def test_torn_document_does_not_invent_fields():
    res = extract_document(torn_lines(), INFO)
    assert res.document_type == "cccd"  # suy ra tu so 12 chu so + ho ten (khong co tieu de)
    assert res.id_number.value == "079201001234" and res.full_name.value == "LÊ MINH TUẤN"
    for name in ("date_of_birth", "gender", "nationality", "hometown", "residence", "expiry_date", "issue_date"):
        assert getattr(res, name) is None
    assert res.missing_fields == [
        "date_of_birth", "gender", "nationality", "hometown", "residence", "expiry_date", "issue_date",
    ]


def test_label_without_value_is_missing():
    lines = layout(["CĂN CƯỚC CÔNG DÂN", "Số: 001099012345", "Quê quán:", "Nơi thường trú:", "Xã A, Huyện B"])
    res = extract_document(lines, INFO)
    assert res.hometown is None  # dong ke tiep la nhan khac -> khong lay nham dia chi thuong tru
    assert res.residence.value == "Xã A, Huyện B"
    assert "hometown" in res.missing_fields


def test_invalid_date_is_rejected():
    lines = layout(["CĂN CƯỚC CÔNG DÂN", "Số: 001099012345", "Ngày sinh: 31/02/1999"])
    res = extract_document(lines, INFO)
    assert res.date_of_birth is None and "date_of_birth" in res.missing_fields


def test_low_confidence_lines_are_ignored_but_kept_in_raw_texts():
    lines = layout(["CĂN CƯỚC CÔNG DÂN", "Số: 001099012345", ("Họ và tên: AAA BBB", 0.2)])
    res = extract_document(lines, INFO, min_confidence=0.5)
    assert res.full_name is None and "Họ và tên: AAA BBB" in res.raw_texts


# ------------------------------------------------------------------ 4. nhieu OCR

def test_noisy_ocr_confusions_and_glued_separators():
    res = extract_document(noisy_lines(), INFO)
    assert res.document_type == "cccd"
    v = values(res)
    assert v["id_number"] == "001205001234"
    assert v["full_name"] == "NGUYEN VAN C"
    assert v["date_of_birth"] == "01/05/1990"
    assert v["gender"] == "Nữ"
    assert v["hometown"] == "Ha Noi"
    assert res.id_number.raw_value == "O0I2O5OO1234"
    # co sua loi -> confidence bi tru nhe so voi confidence dong goc (0.9)
    assert res.id_number.confidence < 0.9


@pytest.mark.parametrize(
    "text,expected",
    [
        ("01/O5/1990", "01/05/1990"),
        ("1-5-1990", "01/05/1990"),
        ("O1 / 05 / 199O", "01/05/1990"),
        ("Ngày 15 tháng 03 năm 2010", "15/03/2010"),
        ("30/02/1990", None),
        ("abc", None),
    ],
)
def test_parse_date(text, expected):
    got = parse_date(text)
    assert (got[0] if got else None) == expected


def test_find_id_numbers():
    assert [x[0] for x in find_id_numbers("001 099 012 345")] == ["001099012345"]
    assert [x[0] for x in find_id_numbers("So: 0l2345678")] == ["012345678"]
    assert find_id_numbers("Số điện thoại 0912345678") == []        # 10 so
    assert find_id_numbers("IDVNM0010990123<<<<") == []              # dong MRZ
    assert find_id_numbers("DISSIDOBSOS") == []                      # toan chu cai


# ------------------------------------------------------------------ 5. khong phai giay to

def test_non_id_document_returns_unknown_and_nothing_else():
    res = extract_document(non_id_lines(), INFO)
    assert res.document_type == "unknown" and not res.is_valid_document
    assert all(getattr(res, n) is None for n in FIELD_NAMES)
    assert res.missing_fields == list(FIELD_NAMES)
    assert res.overall_confidence == 0.0
    assert len(res.raw_texts) == 4


def test_empty_input():
    res = extract_document([], INFO)
    assert res.document_type == "unknown" and res.raw_texts == [] and res.missing_fields == list(FIELD_NAMES)


# ------------------------------------------------------------------ 6. NFC / khoang trang

def test_clean_text_nfc_and_whitespace():
    nfd = unicodedata.normalize("NFD", "  Họ   và tên \r\n NGUYỄN\tVĂN  ")
    out = clean_text(nfd)
    assert out == unicodedata.normalize("NFC", "Họ và tên \n NGUYỄN VĂN")
    assert unicodedata.is_normalized("NFC", out)


def test_extraction_normalizes_unicode_and_spaces():
    nfd = lambda s: unicodedata.normalize("NFD", s)
    lines = layout([
        nfd("CĂN CƯỚC CÔNG DÂN"),
        "Số:   001099012345  ",
        nfd("Họ  và   tên:   NGUYỄN   VĂN   AN\r\n"),
        nfd("Nơi thường trú:   Số 1,   Hà Nội"),
    ])
    res = extract_document(lines, INFO)
    assert res.document_type == "cccd"
    assert res.full_name.value == "NGUYỄN VĂN AN"
    assert res.residence.value == "Số 1, Hà Nội"
    assert all(unicodedata.is_normalized("NFC", t) for t in res.raw_texts)
    assert all("  " not in t and t == t.strip() for t in res.raw_texts)


# ------------------------------------------------------------------ khac

def test_driver_license():
    lines = layout([
        "GIẤY PHÉP LÁI XE / DRIVER'S LICENSE",
        "Số/No: 790012345678",
        "Họ tên/Name: HOÀNG VĂN EM",
        "Sinh ngày/Date of birth: 20/10/1985",
        "Nơi cư trú/Address: Phường 3, Quận 10, TP Hồ Chí Minh",
        "Hạng/Class: B2",
        "Có giá trị đến/Expires: 20/10/2030",
    ])
    res = extract_document(lines, INFO)
    assert res.document_type == "driver_license"
    v = values(res)
    assert v["id_number"] == "790012345678" and v["full_name"] == "HOÀNG VĂN EM"
    assert v["date_of_birth"] == "20/10/1985" and v["expiry_date"] == "20/10/2030"
    assert v["residence"] == "Phường 3, Quận 10, TP Hồ Chí Minh"


def test_issue_date_from_back_side_text():
    lines = layout(["CHỨNG MINH NHÂN DÂN", "Ngày 15 tháng 03 năm 2010", "Số: 012345678"])
    res = extract_document(lines, INFO)
    assert res.issue_date.value == "15/03/2010"


def test_extract_document_info_maps_box_to_original_image():
    class FakeEngine:
        def read(self, bgr):
            return [
                OcrLine("CĂN CƯỚC CÔNG DÂN", 0.99, (10, 5, 200, 25)),
                OcrLine("Số: 001099012345", 0.97, (10, 30, 210, 50)),
            ]

    img = LoadedImage(bgr=np.zeros((100, 250, 3), np.uint8), original_width=500, original_height=200, scale=0.5)
    res = extract_document_info(img, FakeEngine(), 0.5)
    assert (res.image.width, res.image.height) == (500, 200)
    assert res.id_number.value == "001099012345"
    b = res.id_number.box
    assert (b.x_min, b.y_min, b.x_max, b.y_max) == (20, 60, 420, 100)  # nhan doi (scale 0.5)
