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
