# AI Service - Trí Tuệ Nhân Tạo Nhận Dạng & Đối Chiếu Hồ Sơ

## 1. Tổng quan
Microservice AI được phát triển bằng **Python (FastAPI)**, đảm nhận các tác vụ xử lý thị giác máy tính và thuật toán chấm điểm đối chiếu thông tin tự động.

## 2. 4 Module Trọng Tâm
1. **Module Nhận diện biển số xe (OCR License Plate):**
   - Định vị và cắt vùng biển số từ hình ảnh hiện trường tai nạn.
   - Nhận diện ký tự biển số bằng OCR phục vụ tra cứu chủ phương tiện.
2. **Module Phân tích hình ảnh:**
   - Nhận diện đối tượng người, phương tiện và các đặc điểm nhận dạng nổi bật (hình xăm, trang phục, vết tích).
3. **Module So khớp hình ảnh (Face Matching):**
   - Phát hiện khuôn mặt (Face Detection), căn chỉnh (Face Alignment) và trích xuất đặc trưng vector (Face Embeddings).
   - So sánh khoảng cách cosine giữa ảnh người mất tích và ảnh hồ sơ thi thể.
4. **Module AI Matching hồ sơ (Lõi nghiệp vụ quan trọng nhất):**
   - Tính toán điểm tương đồng tổng hợp dựa trên 5 nhóm tiêu chuẩn:
     $$\text{Matching Score} = 0.30 \times S_{\text{personal}} + 0.25 \times S_{\text{image}} + 0.20 \times S_{\text{features}} + 0.15 \times S_{\text{location}} + 0.10 \times S_{\text{time}}$$
   - Phân loại ngưỡng:
     - `> 50%`: Có khả năng trùng khớp.
     - `> 75%`: Cần xác minh.
     - `> 90%`: Ưu tiên xác minh khẩn.

## 3. Cấu trúc thư mục
```
ai-service/
├── app/
│   ├── api/                # Endpoints FastAPI: face_routes, image_routes, ocr_routes
│   ├── services/           # Logic xử lý: face_matching, image_enhancement, ocr_service
│   ├── models/             # Trọng số mô hình AI (Face model, Image model)
│   ├── schemas/            # Pydantic schemas cho dữ liệu vào/ra
│   ├── core/               # Cấu hình hệ thống, ngưỡng điểm
│   └── utils/              # Tiện ích tiền xử lý ảnh, trích xuất ma trận đặc trưng
├── tests/                  # Kiểm thử thuật toán AI
├── main.py                 # Khởi chạy server FastAPI
└── requirements.txt        # Danh sách thư viện phụ thuộc
```
--------------------------------------------------------------------------


AI Service - Trí Tuệ Nhân Tạo Nhận Dạng & Đối Chiếu Hồ Sơ
1. Tổng quan

Microservice AI được phát triển bằng Python (FastAPI), đảm nhận các tác vụ xử lý thị giác máy tính và thuật toán chấm điểm đối chiếu thông tin tự động.

2. 4 Module Trọng Tâm
Module Nhận diện biển số xe (OCR License Plate):
Định vị và cắt vùng biển số từ hình ảnh hiện trường tai nạn.
Nhận diện ký tự biển số bằng OCR phục vụ tra cứu chủ phương tiện.
Module Phân tích hình ảnh:
Nhận diện đối tượng người, phương tiện và các đặc điểm nhận dạng nổi bật (hình xăm, trang phục, vết tích).
Module So khớp hình ảnh (Face Matching):
Phát hiện khuôn mặt (Face Detection), căn chỉnh (Face Alignment) và trích xuất đặc trưng vector (Face Embeddings).
So sánh khoảng cách cosine giữa ảnh người mất tích và ảnh hồ sơ thi thể.
Module AI Matching hồ sơ (Lõi nghiệp vụ quan trọng nhất):
Tính toán điểm tương đồng tổng hợp dựa trên 5 nhóm tiêu chuẩn:
$$\text{Matching Score} = 0.30 \times S_{\text{personal}} + 0.25 \times S_{\text{image}} + 0.20 \times S_{\text{features}} + 0.15 \times S_{\text{location}} + 0.10 \times S_{\text{time}}$$
Phân loại ngưỡng:
> 50%: Có khả năng trùng khớp.
> 75%: Cần xác minh.
> 90%: Ưu tiên xác minh khẩn.

Hướng dẫn cài đặt và chạy (OCR biển số + phân tích hình ảnh)

Phạm vi đã triển khai: Module 1 (OCR biển số) và Module 2 (phân tích hình ảnh). Module 3 (Face Matching) và Module 4 (AI Matching hồ sơ) chưa triển khai (face_routes.py, face_matching.py, face_schema.py còn để trống).

1. Yêu cầu
Python 3.10 - 3.13, pip. Không cần GPU, không cần cài Tesseract.
Lần cài đầu cần mạng (tải torch, onnxruntime, ...: khoảng 1 GB, chủ yếu do torch).
2. Cài đặt
bash
cd ai-service
python -m venv .venv
# Linux/macOS:
source .venv/bin/activate
# Windows (PowerShell):  .venv\Scripts\Activate.ps1
pip install --upgrade pip
pip install -r requirements.txt

Trọng số OCR đi kèm gói rapidocr. Trọng số nhận diện đối tượng đặt tại app/models/image_model/yolov8n.pt (đã có trong gói bàn giao). Nếu thiếu, tải về:

bash
curl -L -o app/models/image_model/yolov8n.pt \
  https://github.com/ultralytics/assets/releases/download/v8.3.0/yolov8n.pt
3. Chạy service
bash
uvicorn main:app --host 0.0.0.0 --port 8000
Khởi động mất vài giây vì nạp sẵn mô hình (log models preloaded).
Tài liệu Swagger tự sinh: http://localhost:8000/docs
Kiểm tra sống: curl http://localhost:8000/health
Chạy bằng --workers N thì mỗi worker nạp một bản mô hình riêng (tốn RAM); mặc định dùng 1 worker.
4. Hợp đồng API

Mọi endpoint nhận multipart/form-data với một trong hai trường:

Trường	Kiểu	Mô tả
file	file	Ảnh tải lên (JPEG, PNG, WEBP, BMP; tối đa 10 MB)
image_path	text	Đường dẫn ảnh tương đối so với thư mục AI_IMAGE_BASE_DIR (mặc định tắt)

Nếu gửi cả hai thì dùng file.

Phản hồi luôn có cùng khung:

json
{ "success": true, "request_id": "6ab1b9d293cf405a", "processing_ms": 503, "data": { }, "error": null }

Khi lỗi: success=false, data=null, error={"code","message"}. Header X-Request-ID trùng request_id (dùng để truy vết log).

4.1 POST /api/v1/ocr/license-plate
bash
curl -F "file=@plate.jpg" http://localhost:8000/api/v1/ocr/license-plate

data:

json
{
  "image": {"width": 720, "height": 480},
  "found": true,
  "plate": "29A-123.45",
  "plate_compact": "29A12345",
  "province_code": "29",
  "confidence": 1.0,
  "is_valid_format": true,
  "candidates": [
    {"raw_text": "29A-123.45", "plate": "29A-123.45", "is_valid_format": true,
     "confidence": 1.0, "box": {"x_min": 163, "y_min": 321, "x_max": 500, "y_max": 380}}
  ]
}
plate: biển số đã chuẩn hóa (29A-123.45, 29B1-123.45, 51F-1234...). plate_compact bỏ dấu để tra cứu.
found=false nghĩa là không đọc được chuỗi nào đúng định dạng biển số Việt Nam; candidates có thể chứa văn bản thô để tham khảo. Đây là kết quả hợp lệ (HTTP 200), không phải lỗi.
Tọa độ box tính trên ảnh gốc. Hỗ trợ biển 1 dòng và 2 dòng. Lỗi nhầm lẫn OCR (O/0, I/1, B/8, S/5...) được sửa theo vị trí ký tự; kết quả cần người xác minh khi confidence thấp.
4.2 POST /api/v1/image/analyze
bash
curl -F "file=@scene.jpg" http://localhost:8000/api/v1/image/analyze

data:

json
{
  "image": {"width": 810, "height": 1080},
  "objects": [
    {"label": "person", "category": "person", "confidence": 0.8657,
     "box": {"x_min": 48, "y_min": 398, "x_max": 245, "y_max": 902},
     "attributes": {"upper_color": "orange", "lower_color": "black"}},
    {"label": "bus", "category": "vehicle", "confidence": 0.8734,
     "box": {"x_min": 22, "y_min": 231, "x_max": 805, "y_max": 756},
     "attributes": {"color": "blue"}}
  ],
  "summary": {"person": 3, "vehicle": 1}
}
label: person, bicycle, car, motorcycle, bus, truck. attributes: màu áo/quần (người), màu thân xe (phương tiện), gồm black, white, gray, red, orange, yellow, green, cyan, blue, purple, pink, unknown. Đây là ước lượng từ vùng ảnh, không chính xác với ảnh thiếu sáng.
Hình xăm, vết sẹo chưa được hỗ trợ: cần mô hình huấn luyện riêng (đặt trọng số trong app/models/image_model/ và mở rộng object_detection.py).
4.3 Mã lỗi
HTTP	error.code	Nguyên nhân
400	MISSING_IMAGE	Không gửi file hay image_path
400	INVALID_IMAGE	Không phải ảnh hợp lệ, quá nhỏ (< 32 px) hoặc không giải mã được
403	IMAGE_PATH_FORBIDDEN	image_path ngoài thư mục cho phép, hoặc chế độ này đang tắt
404	IMAGE_NOT_FOUND	Không có ảnh tại image_path
413	IMAGE_TOO_LARGE	Quá dung lượng hoặc quá số pixel cho phép
415	UNSUPPORTED_MEDIA_TYPE	Định dạng ảnh không được hỗ trợ
422	VALIDATION_ERROR	Tham số yêu cầu sai
503	MODEL_UNAVAILABLE	Thiếu file trọng số mô hình
504	PROCESSING_TIMEOUT	Xử lý quá thời gian hoặc hàng đợi đầy
500	INTERNAL_ERROR	Lỗi không dự kiến (chi tiết chỉ nằm trong log)
5. Cấu hình (biến môi trường tiền tố AI_, hoặc file .env)

Xem .env.example. Các biến chính:

Biến	Mặc định	Ý nghĩa
AI_IMAGE_BASE_DIR	(tắt)	Thư mục gốc cho phép đọc ảnh theo image_path
AI_MAX_IMAGE_MB	10	Dung lượng ảnh tối đa
AI_REQUEST_TIMEOUT_S	20	Giới hạn thời gian mỗi yêu cầu (kể cả thời gian chờ hàng đợi)
AI_MAX_CONCURRENT_JOBS	2	Số tác vụ AI chạy đồng thời
AI_OCR_MIN_CONFIDENCE	0.5	Bỏ dòng OCR có độ tin cậy thấp hơn
AI_DETECTOR_MIN_CONFIDENCE	0.35	Ngưỡng nhận diện đối tượng
AI_PRELOAD_MODELS	true	Nạp mô hình lúc khởi động
AI_LOG_LEVEL	INFO	Mức log

Lưu ý: khi hết thời gian, yêu cầu trả 504 nhưng luồng tính toán đang chạy không thể ngắt giữa chừng; chỗ chạy chỉ được giải phóng khi nó kết thúc thật, nên service không bị quá tải.

6. Bảo mật và log
Log chỉ ghi request_id, phương thức, đường dẫn endpoint, mã trạng thái, thời gian và số lượng kết quả. Không ghi biển số, tên file, đường dẫn hay nội dung ảnh.
Kiểm tra định dạng bằng nội dung thật của tệp (không tin Content-Type/đuôi file), giới hạn dung lượng và số pixel, ảnh quá lớn được thu nhỏ (cạnh dài 1600 px) trước khi xử lý.
image_path chỉ đọc trong AI_IMAGE_BASE_DIR, chặn .. và đường dẫn tuyệt đối.
Service chưa có xác thực: chỉ mở trong mạng nội bộ hoặc đặt sau backend/API gateway.
7. Kiểm thử
bash
python -m pytest -q

Test dùng engine giả lập nên chạy nhanh, không cần mô hình. Gồm: chuẩn hóa biển số, kiểm tra ảnh đầu vào, điều phối OCR, API (lỗi, timeout, chặn path traversal, log không lộ dữ liệu).

8. Gọi thử bằng Postman

Import postman_collection.json (đặt biến baseUrl = http://localhost:8000), hoặc tạo request POST -> Body -> form-data -> khóa file (kiểu File).

9. Xử lý sự cố thường gặp
pip check báo ultralytics requires opencv-python: bỏ qua được, service dùng opencv-python-headless. Nếu import cv2 lỗi do hai gói OpenCV đè nhau: pip uninstall -y opencv-python opencv-python-headless && pip install opencv-python-headless.
Lỗi MODEL_UNAVAILABLE: thiếu app/models/image_model/yolov8n.pt (xem mục 2).
Yêu cầu đầu tiên chậm hơn (khởi tạo ONNX Runtime) khi tắt AI_PRELOAD_MODELS.