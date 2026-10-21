"""Cau hinh log. Quy uoc: KHONG ghi noi dung nhan dang (bien so, ten file, duong dan,
noi dung anh). Chi ghi request_id, endpoint, kich thuoc, thoi gian, so luong ket qua."""
import logging
import sys


def setup_logging(level: str = "INFO") -> None:
    root = logging.getLogger()
    root.handlers.clear()
    handler = logging.StreamHandler(sys.stdout)
    handler.setFormatter(
        logging.Formatter("%(asctime)s %(levelname)s %(name)s %(message)s")
    )
    root.addHandler(handler)
    root.setLevel(level.upper())
    # Thu vien ben thu ba hay in duong dan/chi tiet, ha xuong WARNING
    for noisy in ("RapidOCR", "ultralytics", "multipart", "PIL", "httpx"):
        logging.getLogger(noisy).setLevel(logging.WARNING)
