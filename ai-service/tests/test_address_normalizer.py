"""Kiem thu chuan hoa chuoi dia chi. Khong can mang/FastAPI."""
import unittest
from types import SimpleNamespace as Line

from app.services.address_normalizer import address_text_from_ocr_lines, fold, normalize_address


class NormalizeTests(unittest.TestCase):
    def check(self, raw, expected):
        self.assertEqual(normalize_address(raw).text, expected)

    def test_full_address_whitespace_and_punctuation(self):
        self.check(
            "  12  Nguyễn Huệ ,P. Bến Nghé,Q.1 , TP.HCM ",
            "12 Nguyễn Huệ, Phường Bến Nghé, Quận 1, Thành phố Hồ Chí Minh",
        )

    def test_all_caps_ocr_output(self):
        self.check(
            "SỐ 25 ĐƯỜNG LÊ LỢI PHƯỜNG 5 QUẬN 3 TPHCM",
            "25 Đường Lê Lợi, Phường 5, Quận 3, Thành phố Hồ Chí Minh",
        )

    def test_label_prefix_and_dash_separators(self):
        self.check(
            "Đ/c: 45 Trần Hưng Đạo - P5 - Q.Tân Bình - HCM",
            "45 Trần Hưng Đạo, Phường 5, Quận Tân Bình, Thành phố Hồ Chí Minh",
        )

    def test_no_diacritics(self):
        self.check(
            "so 7 duong so 3 phuong 4 quan 8 thanh pho ho chi minh",
            "7 Duong So 3, Phường 4, Quận 8, Thành phố Hồ Chí Minh",
        )

    def test_multiline_parenthesis_country_and_highway(self):
        n = normalize_address("Km 12 QL1A, X. Tân Phú, H. Củ Chi (gần chợ)\nThành phố Hồ Chí Minh, Việt Nam")
        self.assertEqual(n.text, "Km 12 Quốc lộ 1A, Xã Tân Phú, Huyện Củ Chi, Thành phố Hồ Chí Minh")
        self.assertTrue(any("ngoặc" in note for note in n.notes))

    def test_house_number_with_slash_and_hem_kept(self):
        n = normalize_address("12/3A Hẻm 45 Lê Văn Việt KP.3 TX.Dĩ An Bình Dương")
        self.assertEqual(n.parts[0], "12/3A Hẻm 45 Lê Văn Việt")
        self.assertIn("Khu phố 3", n.parts)

    def test_street_names_containing_admin_words_not_split(self):
        self.check("Phố Xã Đàn, Đống Đa, HN", "Phố Xã Đàn, Đống Đa, Hà Nội")
        self.check("Đường Tỉnh Lộ 10, Bình Chánh", "Đường Tỉnh Lộ 10, Bình Chánh")
        self.check("Đường Số 7, Phường 4", "Đường Số 7, Phường 4")  # "Số" giua phan khong bi bo

    def test_duplicated_admin_word_collapsed(self):
        self.check("Thành phố Thành phố HCM", "Thành phố Hồ Chí Minh")

    def test_empty_and_noise_inputs(self):
        for raw in (None, "", "   ", "---  |||  ", "Việt Nam", "\n\t"):
            n = normalize_address(raw)
            self.assertTrue(n.is_empty, repr(raw))
            self.assertEqual(n.fallback_queries(), [])

    def test_very_long_input_is_truncated(self):
        n = normalize_address("a" * 50_000)
        self.assertLessEqual(len(n.raw), 1000)

    def test_idempotent(self):
        once = normalize_address("12 nguyen hue, p.ben nghe, q1, tphcm").text
        self.assertEqual(normalize_address(once).text, once)

    def test_legacy_level_note(self):
        self.assertTrue(normalize_address("Quận 1, TPHCM").notes)
        self.assertFalse(normalize_address("Phố Xã Đàn, Hà Nội").notes)


class FoldTests(unittest.TestCase):
    def test_fold(self):
        self.assertEqual(fold("Đường Nguyễn  Huệ,"), "duong nguyen hue")
        self.assertEqual(fold("PHƯỜNG 5"), "phuong 5")


class FallbackTests(unittest.TestCase):
    def test_progressively_drops_most_specific_part(self):
        n = normalize_address("12 Nguyễn Huệ, Phường Bến Nghé, Quận 1, Thành phố Hồ Chí Minh")
        qs = n.fallback_queries(3)
        self.assertEqual(qs[0].dropped, ())
        self.assertEqual(qs[1].query, "Phường Bến Nghé, Quận 1, Thành phố Hồ Chí Minh")
        self.assertEqual(qs[1].dropped, ("12 Nguyễn Huệ",))
        self.assertEqual(qs[2].query, "Quận 1, Thành phố Hồ Chí Minh")

    def test_never_reduces_to_a_single_part(self):
        self.assertEqual(len(normalize_address("Quận 1, Thành phố Hồ Chí Minh").fallback_queries(5)), 1)
        self.assertEqual(len(normalize_address("Nguyễn Huệ").fallback_queries(5)), 1)

    def test_max_attempts_respected(self):
        n = normalize_address("a1, b2, c3, d4, e5, f6")
        self.assertEqual(len(n.fallback_queries(2)), 2)


class OcrLinesTests(unittest.TestCase):
    def test_sorted_top_to_bottom_and_low_confidence_dropped(self):
        lines = [
            Line(text="Q1, TPHCM", confidence=0.9, box=(0, 50, 10, 60)),
            Line(text="12 Nguyễn Huệ", confidence=0.9, box=(0, 10, 10, 20)),
            Line(text="nhiễu", confidence=0.2, box=(0, 0, 1, 1)),
            Line(text="   ", confidence=0.99, box=(0, 70, 1, 71)),
        ]
        self.assertEqual(address_text_from_ocr_lines(lines, 0.5), "12 Nguyễn Huệ, Q1, TPHCM")

    def test_keeps_input_order_without_boxes(self):
        lines = [Line(text="B", confidence=1.0), Line(text="A", confidence=1.0)]
        self.assertEqual(address_text_from_ocr_lines(lines), "B, A")

    def test_empty(self):
        self.assertEqual(address_text_from_ocr_lines([]), "")


if __name__ == "__main__":
    unittest.main()
