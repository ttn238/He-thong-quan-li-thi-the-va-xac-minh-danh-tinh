"""Smoke test PaddleOCR THAT, chay qua dung engine + dieu phoi bien so cua AI Service (task 5.6).

    cd ai-service
    pip install -r requirements.txt && pip install -r requirements-paddle.txt
    python scripts/paddle_smoke_test.py                 # anh tong hop
    python scripts/paddle_smoke_test.py anh1.jpg ...    # them anh that

Lan dau can Internet de tai mo hinh (~vai tram MB). Ket qua in ra la ket qua chay that cua may ban,
khong co so lieu dien san. Anh tong hop chi la chu ASCII, khong dai dien anh hien truong.
"""
import sys
import time
from pathlib import Path

import cv2
import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from app.core.config import get_settings  # noqa: E402
from app.services.ocr_service import recognize_plate  # noqa: E402
from app.services.paddle_ocr_engine import PaddleOcrEngine  # noqa: E402
from app.utils.image_processing import decode_image  # noqa: E402


def synthetic() -> bytes:
    img = np.full((200, 640, 3), 255, np.uint8)
    cv2.putText(img, "51F-123.45", (40, 120), cv2.FONT_HERSHEY_SIMPLEX, 2.4, (0, 0, 0), 4, cv2.LINE_AA)
    return cv2.imencode(".png", img)[1].tobytes()


def main() -> int:
    s = get_settings()
    engine = PaddleOcrEngine(s.paddle_lang, s.paddle_device, s.paddle_det_model, s.paddle_rec_model)
    cases = {"tong_hop (ky vong 51F-123.45)": synthetic()}
    cases.update({p: Path(p).read_bytes() for p in sys.argv[1:]})
    ok = True
    for name, data in cases.items():
        print(f"== {name}")
        try:
            image = decode_image(data, s)
            t = time.perf_counter()
            lines = engine.read(image.bgr)
            print(f"  engine.read: {len(lines)} dong, {time.perf_counter() - t:.1f}s")
            for l in lines:
                print(f"    {l.text!r} conf={l.confidence:.4f} box={tuple(round(v) for v in l.box)}")
            t = time.perf_counter()
            res = recognize_plate(image, engine, s.ocr_min_confidence)
            print(f"  recognize_plate: found={res.found} plate={res.plate} compact={res.plate_compact} "
                  f"conf={res.confidence} valid={res.is_valid_format} ({time.perf_counter() - t:.1f}s)")
        except Exception as exc:  # noqa: BLE001
            ok = False
            print(f"  LOI: {type(exc).__name__}: {exc}")
    print("Trang thai engine:", engine.status())
    print("Luu y: ket qua OCR chua duoc xac minh, can nguoi kiem tra.")
    return 0 if ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
