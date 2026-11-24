# Tài Liệu API Hệ Thống (RESTful API Documentation)

## Danh sách thi thể task 6.2

WinForms mở **Quản lý hồ sơ → Danh sách hồ sơ thi thể**. Màn hình dùng `BodyService` và SQL Server thật. Các bộ lọc ngày trên giao diện bao gồm toàn bộ ngày kết thúc.

Endpoint đã triển khai là `GET /api/bodies`. Chạy cùng chương trình ở chế độ HTTP riêng bằng lệnh sau từ thư mục gốc repo.

```powershell
dotnet run --project backend/src/DeathManagement.API -- --body-api --urls http://127.0.0.1:5086
```

Chế độ này cần .NET 10 ASP.NET Core và Windows Desktop runtime. Chạy WinForms và HTTP host với cùng `Jwt__Key`, `Jwt__Issuer` và `Jwt__Audience` để dùng token đăng nhập desktop. Nếu khóa dưới 32 byte, cơ chế JWT hiện tại sinh khóa ngẫu nhiên riêng cho từng tiến trình, nên token không dùng chéo được. Chuỗi kết nối đọc từ `appsettings.json` trong output và biến `ConnectionStrings__DefaultConnection`.

Gửi `Authorization: Bearer <token>` với quyền `BODY:VIEW`. Các tham số query là `tuKhoa`, `trangThai`, `gioiTinh`, `tuNgay`, `denNgay`, `trang` (mặc định 1), `soLuong` (mặc định 20, tối đa 100). Ngày theo ISO 8601, hai giới hạn được tính bao gồm chính thời điểm truyền vào. Từ khóa tìm theo chuỗi mã hồ sơ, địa điểm hoặc đặc điểm. Sắp xếp mã hồ sơ giảm dần để phân trang ổn định.

```text
GET /api/bodies?tuKhoa=1&trangThai=MoiTiepNhan&gioiTinh=Nam&tuNgay=2020-01-01T00:00:00&denNgay=2026-12-31T23:59:59&trang=1&soLuong=20
```

JSON trả về có `items`, `total`, `trang`, `soLuong`. Mỗi hồ sơ có mã, thời gian phát hiện, trạng thái, giới tính và tuổi ước lượng. Quyền đầy đủ nhận thêm địa điểm, đặc điểm, chiều cao và mã tin báo. Với `NHA_XAC` hoặc quyền `PARTIAL`, truy vấn sử dụng `v_HoSoThiTheNhaXac`, trả thêm mã vị trí, khu, tủ, ngăn. Các trường ngoài view có giá trị null và được bỏ khỏi JSON HTTP. Tìm kiếm và tổng số của nhóm này chỉ dựa trên mã hồ sơ và các bộ lọc công khai, không dựa vào dữ liệu bị ẩn.

HTTP 401 khi thiếu/token sai, 403 khi thiếu quyền hoặc phạm vi OWN chưa được hỗ trợ, 400 khi bộ lọc sai và 200 với `items=[]` khi không có kết quả. Endpoint dùng riêng HTTP host của chương trình WinForms, không nằm trong `DeathManagement.ImageApi`.

Kiểm thử hồi quy nằm tại `tests/backend/DeathManagement.Tests/BodyListTests.cs`. Ca SQL Server chỉ thực hiện truy vấn đọc khi đặt `QLTT_TEST_DB`.

```powershell
$env:QLTT_TEST_DB='Server=(localdb)\MSSQLLocalDB;Database=QL_XACMINHTHITHE;Trusted_Connection=True;TrustServerCertificate=True'
dotnet test tests/backend/DeathManagement.Tests/DeathManagement.Tests.csproj --filter FullyQualifiedName~BodyListTests
```

Đã xác minh ngày 20/11/2026 với 5 ca đạt khi bật kiểm tra LocalDB. HTTP thực tế trả 200 cho cán bộ và nhà xác, 403 cho token thiếu quyền, 401 cho truy cập ẩn danh. JSON nhà xác không chứa `diaDiem`. Bộ lọc ngày, trạng thái, giới tính, mã và phân trang đã truy vấn được trên dữ liệu LocalDB.

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

### 2.9. AI Service – Tra cứu địa điểm (`POST /api/v1/location/search`)

> Endpoint của **ai-service** (FastAPI, mặc định `http://localhost:8000`), chỉ mở trong mạng nội bộ. Backend chưa tích hợp; khi tích hợp, backend gọi endpoint này rồi trả kết quả cho client. Endpoint nhận **chuỗi văn bản** (chuỗi do OCR đọc ra hoặc địa chỉ nhập tay) và trả về **danh sách gợi ý**; kết quả **không bao giờ được coi là đã xác minh**.

**Request** (`application/json`)

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `text` | string (1–300 ký tự) | Có | Chuỗi địa điểm/địa chỉ cần tra cứu |
| `limit` | int (1–10, mặc định 5) | Không | Số gợi ý tối đa |

```json
{ "text": "12 Nguyễn Huệ, P. Bến Nghé, Q.1, TPHCM", "limit": 5 }
```

**Response** – cùng khung với các endpoint AI khác (`success`, `request_id`, `processing_ms`, `data`, `error`):

```json
{
  "success": true,
  "request_id": "6ab1b9d293cf405a",
  "processing_ms": 1530,
  "data": {
    "query": "12 Nguyễn Huệ, P. Bến Nghé, Q.1, TPHCM",
    "normalized_query": "12 Nguyễn Huệ, Phường Bến Nghé, Quận 1, Thành phố Hồ Chí Minh",
    "found": true,
    "verified": false,
    "match_scope": "partial",
    "used_query": "Phường Bến Nghé, Quận 1, Thành phố Hồ Chí Minh",
    "dropped_parts": ["12 Nguyễn Huệ"],
    "ambiguous": false,
    "candidates": [
      {
        "display_name": "Phường Bến Nghé, Quận 1, Thành phố Hồ Chí Minh, Việt Nam",
        "lat": 10.7769, "lon": 106.7009,
        "match_level": "ward",
        "similarity": 0.667,
        "importance": 0.45,
        "components": { "house_number": null, "road": null, "ward": "Phường Bến Nghé",
                        "district": "Quận 1", "city": "Thành phố Hồ Chí Minh",
                        "province": null, "postcode": null, "country_code": "vn" },
        "source": "nominatim",
        "osm_url": "https://www.openstreetmap.org/relation/2000000"
      }
    ],
    "warnings": ["Không tìm thấy địa chỉ đầy đủ; kết quả chỉ khớp phần còn lại sau khi bỏ: 12 Nguyễn Huệ."],
    "attribution": "Dữ liệu © OpenStreetMap contributors (ODbL), tra cứu qua Nominatim",
    "notice": "Chỉ là gợi ý từ dữ liệu bản đồ mở, chưa được xác minh. Cán bộ cần đối chiếu thực tế trước khi sử dụng."
  },
  "error": null
}
```

| Trường `data` | Ý nghĩa |
|---|---|
| `normalized_query` | Chuỗi sau chuẩn hóa (khoảng trắng, dấu câu, viết tắt P./Q./TP./TPHCM…, dấu phẩy giữa các cấp) |
| `found` | `false` = không có gợi ý nào. Đây là kết quả **hợp lệ (HTTP 200)**, không phải lỗi |
| `verified` | Luôn `false` |
| `match_scope` | `full`: khớp theo địa chỉ đầy đủ; `partial`: phải bỏ bớt phần cụ thể nhất (`dropped_parts`) mới có kết quả. `null` nếu không tìm thấy |
| `ambiguous` | `true` khi có nhiều gợi ý gần như ngang nhau, người dùng phải tự chọn |
| `candidates[].match_level` | Mức chi tiết tìm được: `house`, `street`, `ward`, `district`, `city`, `province`, `country`, `other` |
| `candidates[].similarity` | Độ giống (0–1) giữa chuỗi nhập và kết quả, tính theo từ khóa (ước lượng, **không phải** độ chính xác vị trí) |
| `candidates[].osm_url` | Liên kết để đối chiếu trên OpenStreetMap |
| `warnings` | Cảnh báo cần hiển thị cho người dùng (khớp một phần, nhiều phương án, độ giống thấp, đơn vị hành chính có thể đã thay đổi…) |
| `attribution` | Dòng ghi nguồn OpenStreetMap, **phải hiển thị** cùng kết quả |

**Mã lỗi**

| HTTP | `error.code` | Nguyên nhân |
|---|---|---|
| 400 | `INVALID_ADDRESS` | Chuỗi rỗng hoặc không còn nội dung sau chuẩn hóa (chỉ ký tự nhiễu, chỉ "Việt Nam"…) |
| 422 | `VALIDATION_ERROR` | `text` rỗng/dài quá 300 ký tự, `limit` ngoài 1–10 |
| 503 | `GEOCODING_UNAVAILABLE` | Lỗi mạng, bị giới hạn tốc độ (429/403), nguồn lỗi 5xx hoặc trả dữ liệu không hợp lệ. **Không được hiểu là "không tìm thấy"** |
| 503 | `GEOCODING_DISABLED` | Tính năng bị tắt (`AI_GEOCODER_ENABLED=false`) |
| 504 | `PROCESSING_TIMEOUT` | Xử lý vượt `AI_REQUEST_TIMEOUT_S` |

**Hành vi cần biết**
- Thử địa chỉ đầy đủ trước; nếu không có kết quả thì bỏ dần phần cụ thể nhất (số nhà, đường…), tối đa `AI_GEOCODER_MAX_ATTEMPTS` lần và không bao giờ rút xuống chỉ còn một phần.
- Kết quả trùng (cùng tọa độ hoặc cùng tên) được gộp; xếp theo `similarity` rồi `importance`.
- Nguồn tra cứu mặc định là Nominatim công cộng của OpenStreetMap: tối đa 1 yêu cầu/giây trên toàn dịch vụ, có cache (24 giờ; "không tìm thấy" 5 phút), tạm ngưng gọi 60 giây sau khi bị 429/403/5xx hoặc lỗi mạng. Phù hợp demo/lưu lượng nhỏ; triển khai thật nên dùng máy chủ Nominatim riêng (`AI_GEOCODER_BASE_URL`).
- **Quyền riêng tư:** chính sách Nominatim công cộng không cho phép gửi dữ liệu cá nhân/tài liệu mật, và chuỗi địa điểm được gửi ra dịch vụ bên ngoài. Chỉ gửi chuỗi địa điểm (không kèm tên, CCCD, mã hồ sơ); cân nhắc việc gửi địa điểm của hồ sơ thi thể ra ngoài trước khi bật. Log của dịch vụ không ghi chuỗi địa chỉ.

#### 2.9.1. Tra cứu địa điểm từ ảnh giấy tờ (`POST /api/v1/location/from-document`)

Nhận **ảnh giấy tờ tùy thân** (`multipart/form-data`: `file` hoặc `image_path`, giống `/api/v1/ocr/document`), OCR, lấy trường **nơi cư trú** (`residence`), nếu không có thì **quê quán** (`hometown`), rồi tra cứu như `/location/search`. Kết quả vẫn là gợi ý, `verified` luôn `false`.

`data`:

| Trường | Kiểu | Mô tả |
|---|---|---|
| `document_type` | string | `cccd` / `cmnd` / `driver_license` / `unknown` |
| `address_found` | bool | `false` nếu OCR không đọc được trường địa chỉ nào |
| `source_field` | `residence` \| `hometown` \| null | Trường OCR được dùng |
| `address_text` | string \| null | Chuỗi địa chỉ OCR đưa vào tra cứu (để người dùng đối chiếu với giấy tờ) |
| `extraction_confidence` | number 0–1 | Độ tin cậy OCR của trường địa chỉ |
| `location` | object \| null | Cùng cấu trúc `data` của `/location/search`; `null` khi `address_found=false` |
| `warnings` | string[] | Ví dụ: không đọc được địa chỉ; độ tin cậy OCR thấp (< 0.7) |

Trường hợp không tìm thấy: `address_found=false` (kèm `warnings`, `location=null`) hoặc `location.found=false` (không có gợi ý). Lỗi dùng chung mã của `/location/search` (`GEOCODING_DISABLED`, `GEOCODING_UNAVAILABLE`, ...) và của OCR (ảnh không hợp lệ, quá lớn).
