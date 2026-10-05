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
