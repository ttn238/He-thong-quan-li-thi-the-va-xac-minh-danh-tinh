from pathlib import Path

# Thư mục lưu ảnh và kết quả (ai-service/data, nên thêm vào .gitignore)
DATA_DIR = Path(__file__).resolve().parents[2] / "data"
MAX_UPLOAD_BYTES = 10 * 1024 * 1024          # 10 MB / ảnh
MAX_IMAGE_PIXELS = 50_000_000                # chặn ảnh quá lớn (decompression bomb)
ALLOWED_FORMATS = {"JPEG", "PNG", "WEBP", "BMP"}
ENGINE_NAME = "RapidOCR (PP-OCR ONNX)"

DATA_DIR.mkdir(parents=True, exist_ok=True)