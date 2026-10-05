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
