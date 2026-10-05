# UC04 – Tiếp Nhận & Quản Lý Hồ Sơ Tử Thi Chưa Xác Định Danh Tính

## 1. Thông tin chung
- **Mã Use Case:** UC04
- **Tên Use Case:** Tiếp nhận hồ sơ tử thi chưa xác định danh tính
- **Actor chính:** Cán bộ xác minh
- **Actor phụ:** Hệ thống AI
- **Mục tiêu:** Cán bộ chuyên trách lập hồ sơ quản lý đối với các thi thể tiếp nhận chưa rõ danh tính, phục vụ công tác giám định và truy nguyên danh tính.

## 2. Quy trình xử lý
1. Cán bộ đăng nhập vào hệ thống với vai trò `Officer`.
2. Chọn chức năng "Tiếp nhận hồ sơ tử thi".
3. Nhập các thông tin bắt buộc và chi tiết:
   - **Thông tin vụ việc:** Mã vụ việc, ngày giờ phát hiện, địa điểm phát hiện (tỉnh/thành, quận/huyện, tọa độ), hiện trường.
   - **Thông tin tử thi:** Giới tính ước tính, độ tuổi ước tính, tình trạng thi thể, đặc điểm nhân dạng nổi bật (sẹo, hình xăm, răng giả, dị tật...).
   - **Vật phẩm & Tư trang:** Quần áo, trang sức, giấy tờ tùy thân rách nát, phương tiện kèm theo (biển số xe nếu có).
   - **Hình ảnh:** Tải lên ảnh tử thi, ảnh dấu vết nhận dạng và ảnh vật phẩm (được mã hóa và lưu trữ tại vùng bảo mật).
4. Hệ thống kiểm tra dữ liệu, tự động cấp Mã hồ sơ duy nhất (`MaHoSo`).
5. Lưu bản ghi vào bảng `HoSoChuaXacDinh` và `VuViec`.
6. Hệ thống tự động chuyển giao dữ liệu sang **Phân hệ AI** để phân tích đặc trưng ảnh khuôn mặt và thực hiện matching toàn hệ thống.
7. Ghi nhật ký vào `NhatKyHeThong`.
