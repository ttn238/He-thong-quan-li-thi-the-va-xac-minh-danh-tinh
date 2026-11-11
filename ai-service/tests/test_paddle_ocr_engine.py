"""Kiem thu PaddleOcrEngine (task 5.6).

QUAN TRONG: cac test dung module `paddleocr` GIA (inject vao sys.modules) de kiem thu LOGIC cua engine
(nap lazy, loi nap, cooldown, tham so, chuyen doi ket qua). Chung KHONG chung minh PaddleOCR that nhan dang dung;
viec do can `python -m pytest` + chay OCR that tren may da cai requirements-paddle.txt.
Du lieu mau cua parser dung tay theo dinh dang tai lieu PaddleOCR 2.x/3.x.
"""
import subprocess
import sys
import unittest
from pathlib import Path
from types import ModuleType
from unittest import mock

import numpy as np

from app.core.errors import ModelUnavailableError, OcrFailedError
from app.services import paddle_ocr_engine as pe
from app.services.paddle_ocr_engine import PaddleOcrEngine, parse_v2, parse_v3, polygon_to_xyxy

try:  # cac test duoi can pydantic/fastapi (co trong requirements.txt cua 5.4)
    from app.services.ocr_service import OcrLine, recognize_plate
    HAVE_APP = True
except ImportError:
    HAVE_APP = False

ROOT = Path(__file__).resolve().parents[1]
POLY = [[10, 10], [110, 10], [110, 40], [10, 40]]


def fake_paddleocr(predict=None, ocr=None, ctor_error=None):
    """Tao module `paddleocr` gia; tra ve (module, danh sach tham so da goi constructor)."""
    calls = []

    class PaddleOCR:
        def __init__(self, **kwargs):
            calls.append(kwargs)
            if ctor_error:
                raise ctor_error

        def predict(self, img):
            return predict(img)

        def ocr(self, img, cls=True):
            return ocr(img)

    mod = ModuleType("paddleocr")
    mod.PaddleOCR = PaddleOCR
    return mod, calls


class ParseTests(unittest.TestCase):
    def test_v3_dict_like_and_empty_text_dropped(self):
        raw = [{"rec_texts": ["Hello", "", "World"], "rec_scores": np.array([0.98, 0.5, 0.91]),
                "rec_polys": [np.array(POLY)] * 3}]
        out = parse_v3(raw)
        self.assertEqual([t for t, _, _ in out], ["Hello", "World"])
        self.assertAlmostEqual(out[0][1], 0.98)
        self.assertEqual(out[0][2], [[10.0, 10.0], [110.0, 10.0], [110.0, 40.0], [10.0, 40.0]])

    def test_v3_res_wrapper_json_attr_and_dt_polys_fallback(self):
        wrapped = {"res": {"rec_texts": ["A"], "rec_scores": [0.9], "dt_polys": [POLY]}}
        self.assertEqual(parse_v3([wrapped])[0][0], "A")

        class Obj:
            json = wrapped
        self.assertIsNotNone(parse_v3([Obj()])[0][2])

    def test_v3_missing_fields_not_fabricated(self):
        out = parse_v3([{"rec_texts": ["A", "B"], "rec_scores": [0.9]}])
        self.assertEqual(out[0][1], 0.9)
        self.assertIsNone(out[1][1])
        self.assertIsNone(out[0][2])

    def test_v3_garbage_and_bad_values(self):
        self.assertEqual(parse_v3(None), [])
        self.assertEqual(parse_v3([42, None, {"rec_texts": []}]), [])
        out = parse_v3([{"rec_texts": ["A", "B"], "rec_scores": [float("nan"), 7.0], "rec_polys": [[[1, 2]], "x"]}])
        self.assertIsNone(out[0][1])
        self.assertEqual(out[1][1], 1.0)
        self.assertIsNone(out[0][2])
        self.assertIsNone(out[1][2])

    def test_v2_format_and_empty_shapes(self):
        out = parse_v2([[[POLY, ("Hello", 0.97)]]])
        self.assertEqual(out[0][0], "Hello")
        self.assertAlmostEqual(out[0][1], 0.97)
        for empty in ([None], [[]], None, [[["bad"]]]):
            self.assertEqual(parse_v2(empty), [])

    def test_polygon_to_xyxy(self):
        self.assertEqual(polygon_to_xyxy([[10, 12], [110, 10], [112, 40], [8, 42]]), (8, 10, 112, 42))


class LoadTests(unittest.TestCase):
    def _engine(self, **kw):
        return PaddleOcrEngine(**kw)

    def test_import_module_does_not_load_paddle(self):
        code = "import sys; import app.services.paddle_ocr_engine; print(any(m=='paddle' or m.startswith('paddleocr') for m in sys.modules))"
        out = subprocess.run([sys.executable, "-c", code], cwd=ROOT, capture_output=True, text=True, timeout=60)
        self.assertEqual(out.stdout.strip(), "False", out.stderr)

    def test_v3_kwargs_and_optional_model_names(self):
        mod, calls = fake_paddleocr(predict=lambda img: [])
        with mock.patch.dict(sys.modules, {"paddleocr": mod}), mock.patch("importlib.metadata.version", return_value="3.1.0"):
            self._engine(lang="en", device="cpu").load()
            self._engine(det_model="D", rec_model="R").load()
        k = calls[0]
        self.assertEqual((k["lang"], k["device"]), ("en", "cpu"))
        self.assertIs(k["use_doc_orientation_classify"], False)
        self.assertIs(k["use_doc_unwarping"], False)
        self.assertNotIn("text_detection_model_name", k)  # chi truyen khi duoc cau hinh
        self.assertEqual((calls[1]["text_detection_model_name"], calls[1]["text_recognition_model_name"]), ("D", "R"))

    def test_v2_kwargs(self):
        mod, calls = fake_paddleocr(ocr=lambda img: [])
        with mock.patch.dict(sys.modules, {"paddleocr": mod}), mock.patch("importlib.metadata.version", return_value="2.10.0"):
            self._engine(device="gpu:0").load()
        self.assertIs(calls[0]["use_angle_cls"], True)
        self.assertIs(calls[0]["use_gpu"], True)
        self.assertNotIn("device", calls[0])

    def test_missing_library_gives_model_unavailable(self):
        with mock.patch.dict(sys.modules, {"paddleocr": None}):  # None -> import nem ImportError
            e = self._engine()
            with self.assertRaises(ModelUnavailableError):
                e.load()
        self.assertEqual(e.status()["state"], "unavailable")

    def test_ctor_error_like_real_world_and_cooldown(self):
        err = TypeError("PaddlePredictorOption.__init__() takes 1 positional argument but 2 were given")
        mod, calls = fake_paddleocr(ctor_error=err)
        with mock.patch.dict(sys.modules, {"paddleocr": mod}), mock.patch("importlib.metadata.version", return_value="3.1.0"):
            e = self._engine(retry_cooldown_s=60)
            with self.assertRaises(ModelUnavailableError) as cm:
                e.load()
            self.assertIn("TypeError", cm.exception.message)
            with self.assertRaises(ModelUnavailableError):
                e.load()
            self.assertEqual(len(calls), 1, "trong cooldown khong duoc thu nap lai")
            e2 = self._engine(retry_cooldown_s=0)
            for _ in range(2):
                with self.assertRaises(ModelUnavailableError):
                    e2.load()
            self.assertEqual(len(calls), 3, "cooldown=0 thi moi lan load thu lai")

    def test_load_is_idempotent(self):
        mod, calls = fake_paddleocr(predict=lambda img: [])
        with mock.patch.dict(sys.modules, {"paddleocr": mod}), mock.patch("importlib.metadata.version", return_value="3.1.0"):
            e = self._engine()
            e.load()
            e.load()
        self.assertEqual(len(calls), 1)
        self.assertEqual(e.status()["state"], "loaded")


@unittest.skipUnless(HAVE_APP, "Thieu pydantic/pydantic-settings (requirements.txt cua 5.4): khong chay duoc test nay")
class ReadTests(unittest.TestCase):
    IMG = np.full((60, 200, 3), 255, np.uint8)

    def _engine_with(self, predict):
        mod, _ = fake_paddleocr(predict=predict)
        patches = [mock.patch.dict(sys.modules, {"paddleocr": mod}), mock.patch("importlib.metadata.version", return_value="3.1.0")]
        for p in patches:
            p.start()
            self.addCleanup(p.stop)
        return PaddleOcrEngine()

    def test_read_returns_ocrlines_with_xyxy_box(self):
        e = self._engine_with(lambda img: [{"rec_texts": ["51F-123.45"], "rec_scores": [0.97], "rec_polys": [POLY]}])
        lines = e.read(self.IMG)
        self.assertEqual(len(lines), 1)
        self.assertIsInstance(lines[0], OcrLine)
        self.assertEqual((lines[0].text, lines[0].confidence, lines[0].box), ("51F-123.45", 0.97, (10.0, 10.0, 110.0, 40.0)))

    def test_lines_without_score_or_polygon_are_dropped_not_invented(self):
        e = self._engine_with(lambda img: [{"rec_texts": ["A", "B", "C"], "rec_scores": [0.9, 0.9],
                                            "rec_polys": [POLY, None, POLY]}])
        self.assertEqual([l.text for l in e.read(self.IMG)], ["A"])

    def test_no_text_returns_empty_list(self):
        self.assertEqual(self._engine_with(lambda img: [{"rec_texts": [], "rec_scores": [], "rec_polys": []}]).read(self.IMG), [])

    def test_inference_error_becomes_ocr_failed_without_leaking_details(self):
        def boom(img):
            raise RuntimeError("chi tiet noi bo /duong/dan/rieng")
        with self.assertRaises(OcrFailedError) as cm:
            self._engine_with(boom).read(self.IMG)
        self.assertNotIn("duong", cm.exception.message)

    def test_input_image_not_modified(self):
        img = self.IMG.copy()
        self._engine_with(lambda i: []).read(img)
        self.assertTrue(np.array_equal(img, self.IMG))

    def test_recognize_plate_pipeline_with_paddle_engine(self):
        """Dieu phoi bien so cua 5.4 chay duoc voi PaddleOcrEngine (paddleocr gia)."""
        from app.utils.image_processing import LoadedImage
        e = self._engine_with(lambda img: [{"rec_texts": ["51F-123.45"], "rec_scores": [0.97],
                                            "rec_polys": [[[5, 5], [100, 5], [100, 30], [5, 30]]]}])
        bgr = np.full((200, 640, 3), 255, np.uint8)
        res = recognize_plate(LoadedImage(bgr=bgr, original_width=640, original_height=200, scale=1.0), e, 0.5)
        self.assertTrue(res.found)
        self.assertEqual(res.plate, "51F-123.45")
        self.assertEqual(res.plate_compact, "51F12345")


try:
    from app.api import deps
    from app.core.config import Settings
    HAVE_DEPS = True
except ImportError:
    HAVE_DEPS = False


@unittest.skipUnless(HAVE_DEPS, "Thieu fastapi/pydantic-settings: khong chay duoc test chon engine")
class EngineSelectionTests(unittest.TestCase):
    def tearDown(self):
        deps.get_ocr_engine.cache_clear()

    def _get(self, **kw):
        deps.get_ocr_engine.cache_clear()
        with mock.patch.object(deps, "get_settings", return_value=Settings(**kw)):
            return deps.get_ocr_engine()

    def test_default_is_rapidocr_and_paddle_is_opt_in(self):
        self.assertIsInstance(self._get(), deps.RapidOcrEngine)
        e = self._get(ocr_engine="paddle", paddle_lang="en", paddle_det_model="D")
        self.assertIsInstance(e, PaddleOcrEngine)
        self.assertEqual((e.lang, e.device, e.det_model), ("en", "cpu", "D"))

    def test_invalid_engine_name_rejected_by_settings(self):
        with self.assertRaises(Exception):
            Settings(ocr_engine="tesseract")


if __name__ == "__main__":
    unittest.main()
