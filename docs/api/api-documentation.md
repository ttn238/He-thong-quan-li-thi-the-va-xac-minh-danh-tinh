# Tài Liệu API Hệ Thống (RESTful API Documentation)

## 1. Tiêu chuẩn thiết kế
- **Base URL:** `https://api.domain.vn/api/v1`
- **Định dạng dữ liệu:** `application/json`
- **Xác thực:** Bearer JWT Token (`Authorization: Bearer <token>`)

## 2. Danh mục Endpoints chính

### 2.1. Nhóm Xác thực (`/api/v1/auth`)
- `POST /auth/login`: Đăng nhập hệ thống (User, Officer, Admin).
- `POST /auth/register`: Người dùng đăng ký tài khoản mới.
- `POST /auth/change-password`: Đổi mật khẩu.
- `POST /auth/refresh-token`: Làm mới JWT token.

### 2.2. Nhóm Người dùng & Người thân (`/api/v1/users`)
- `GET /users/me`: Xem thông tin tài khoản hiện tại.
- `PUT /users/me`: Cập nhật thông tin cá nhân.
- `GET /users/relatives`: Lấy danh sách người thân đã khai báo.
- `POST /users/relatives`: Thêm mới người thân.
- `PUT /users/relatives/{id}`: Sửa thông tin người thân.
- `DELETE /users/relatives/{id}`: Xóa thông tin người thân.

### 2.3. Nhóm Hồ sơ Người mất tích (`/api/v1/missing-persons`)
- `GET /missing-persons`: Tra cứu danh sách hồ sơ người mất tích (hỗ trợ phân trang, lọc).
- `POST /missing-persons`: Tạo mới hồ sơ người mất tích (gửi thông tin, upload ảnh).
- `GET /missing-persons/{id}`: Chi tiết hồ sơ người mất tích.
- `PUT /missing-persons/{id}`: Cập nhật hồ sơ người mất tích.

### 2.4. Nhóm Hồ sơ Tử thi (`/api/v1/bodies`)
- `GET /bodies`: Danh sách hồ sơ thi thể (chỉ cán bộ/admin truy cập đầy đủ).
- `POST /bodies`: Tiếp nhận và tạo hồ sơ thi thể mới (chỉ Officer).
- `GET /bodies/{id}`: Chi tiết hồ sơ thi thể.
- `PUT /bodies/{id}`: Cập nhật thông tin khám nghiệm/tiếp nhận.
- `POST /bodies/{id}/images`: Tải ảnh thi thể lên vùng lưu trữ an toàn.

### 2.5. Nhóm AI & Matching (`/api/v1/identification`)
- `POST /identification/match`: Kích hoạt đối chiếu 1 hồ sơ với cơ sở dữ liệu.
- `GET /identification/results/{recordId}`: Xem danh sách gợi ý và điểm % matching.
- `POST /identification/ocr-plate`: Yêu cầu AI trích xuất biển số xe từ ảnh hiện trường.
- `POST /identification/face-search`: Tìm kiếm người bằng tải ảnh trực tiếp.

#### API phân tích ảnh (đã triển khai)

Các endpoint dưới đây do `DeathManagement.ImageApi` cung cấp và proxy tới Python AI Service. Tất cả yêu cầu cần `Authorization: Bearer <JWT>` từ hệ thống; backend kiểm tra claim quyền `AI_RESULT`. Không gọi AI Service trực tiếp từ trình duyệt.

| Method | Endpoint | Quyền | Mô tả |
|---|---|---|---|
| `POST` | `/api/v1/identification/images` | `AI_RESULT:VIEW` hoặc `AI_RESULT:CONFIRM` | Nhận multipart form gồm `file` (JPG/PNG/WEBP/BMP, tối đa 10 MB) và `linkedRecordId` tùy chọn; xác thực nội dung, gọi OCR + YOLO và trả kết quả phân tích. |
| `GET` | `/api/v1/identification/images/{requestId}` | `AI_RESULT:VIEW` hoặc `AI_RESULT:CONFIRM` | Lấy kết quả đã lưu. |
| `GET` | `/api/v1/identification/images/{requestId}/image` | `AI_RESULT:VIEW` hoặc `AI_RESULT:CONFIRM` | Tải ảnh đã phân tích; cần JWT. |
| `POST` | `/api/v1/identification/images/{requestId}/review` | `AI_RESULT:CONFIRM` | Lưu biên bản thẩm định; tên cán bộ do backend lấy từ JWT. |

Kết quả dùng camelCase. Tọa độ `bbox`/`box` là `xMin`, `yMin`, `xMax`, `yMax` theo pixel ảnh gốc; `confidence` nằm trong khoảng 0–1. Các địa điểm hiện trả mảng rỗng và trường tương ứng được đánh dấu `unsupported` vì chưa có dữ liệu bản đồ. Mọi dự đoán là gợi ý, cần cán bộ xác minh.

Detector dùng trọng số pretrained `yolov8n.pt` (YOLOv8n, lớp COCO), đã kiểm tra bằng Ultralytics `8.4.197` trên CPU; requirements cho phép `>=8.3,<9`. Ngưỡng confidence mặc định là `0.35` (`AI_DETECTOR_MIN_CONFIDENCE`); lần nạp đầu sẽ tải trọng số chính thức nếu chưa có trong `ai-service/app/models/image_model/`. Nhận diện hiện giới hạn ở người, xe đạp, xe máy, ô tô, xe buýt và xe tải; không xác định danh tính, thương tích hoặc kết luận nghiệp vụ. RapidOCR trả văn bản và độ tin cậy; OCR hiện chưa phân loại biển số/biển đường và không suy luận vị trí.

Ví dụ kết quả rút gọn:

```json
{
  "requestId": "AI-REQ-20261117-1A2B3C4D",
  "status": "completed",
  "engine": "YOLOv8n (COCO) + RapidOCR",
  "image": { "fileName": "hientruong.jpg", "sizeBytes": 1250000, "format": "JPEG", "width": 1920, "height": 1080, "url": "/v1/results/AI-REQ-20261117-1A2B3C4D/image" },
  "ocr": [{ "text": "XUAN LA", "kind": "other", "confidence": 0.91, "bbox": { "xMin": 20, "yMin": 30, "xMax": 150, "yMax": 68 } }],
  "objects": [{ "label": "car", "category": "vehicle", "confidence": 0.88, "box": { "xMin": 200, "yMin": 100, "xMax": 740, "yMax": 550 }, "attributes": {} }],
  "locations": [],
  "missingFields": []
}
```

Backend image API mặc định nghe tại cổng `5000`, AI Service tại `8000`. Cấu hình `Jwt:Key` (biến môi trường `Jwt__Key`) giống nhau cho ứng dụng desktop tạo JWT và Image API; khóa phải có ít nhất 32 byte. Đặt `AiService:BaseUrl` và `AiService:TimeoutSeconds` để cấu hình kết nối và thời gian chờ.

### 2.6. Nhóm Xác minh & Phê duyệt (`/api/v1/verification`)
- `GET /verification/requests`: Lấy danh sách yêu cầu xác nhận cần xử lý.
- `POST /verification/requests`: Người dùng gửi yêu cầu nhận người thân kèm minh chứng.
- `PUT /verification/requests/{id}/approve`: Cán bộ phê duyệt xác nhận danh tính.
- `PUT /verification/requests/{id}/reject`: Cán bộ từ chối kèm lý do.
- `PUT /verification/requests/{id}/request-info`: Cán bộ yêu cầu bổ sung thông tin.

### 2.7. Nhóm Bàn giao (`/api/v1/handover`)
- `POST /handover`: Lập biên bản và hoàn tất thủ tục bàn giao thi thể.
- `GET /handover/{id}`: Xem biên bản bàn giao.

### 2.8. Nhóm Thông báo & Báo cáo (`/api/v1/notifications`, `/api/v1/reports`)
- `GET /notifications`: Xem danh sách thông báo của tài khoản.
- `PUT /notifications/{id}/read`: Đánh dấu thông báo đã đọc.
- `GET /reports/dashboard`: Thống kê số liệu Dashboard cho Admin/Officer.
- `GET /reports/export`: Xuất báo cáo thống kê định dạng PDF hoặc Excel.
