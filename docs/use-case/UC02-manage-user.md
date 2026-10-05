# UC02 – Quản Lý Người Thân

## 1. Thông tin chung
- **Mã Use Case:** UC02
- **Tên Use Case:** Quản lý người thân
- **Actor chính:** Người dùng
- **Mục tiêu:** Cho phép người dùng khai báo, cập nhật và quản lý danh sách người thân trong gia đình để phục vụ công tác tra cứu và bảo vệ thông tin khi cần tìm kiếm.

## 2. Luồng sự kiện chính
1. Người dùng đăng nhập hệ thống và chọn mục "Quản lý người thân".
2. Hệ thống hiển thị danh sách người thân đã khai báo trước đó (nếu có).
3. Người dùng chọn thao tác "Thêm người thân":
   - Nhập: Họ và tên, Ngày tháng năm sinh / Năm sinh, Số CCCD/Định danh (nếu có), Số điện thoại liên hệ, Mối quan hệ (Cha, Mẹ, Vợ, Chồng, Con, Anh/Chị/Em...), Đặc điểm nhận dạng đặc biệt.
4. Người dùng nhấn nút "Lưu".
5. Hệ thống kiểm tra tính hợp lệ dữ liệu.
6. Hệ thống lưu bản ghi vào bảng `NguoiThan` liên kết với tài khoản người dùng và thông báo thành công.

## 3. Luồng rẽ nhánh
- **Chỉnh sửa / Xóa:** Người dùng có thể chọn chỉnh sửa thông tin người thân hoặc xóa thông tin người thân khỏi danh mục quản lý của tài khoản.

## 4. Ngoại lệ
- Nhập thiếu các trường bắt buộc (Họ tên, Mối quan hệ) $\to$ hệ thống đánh dấu đỏ và yêu cầu nhập bổ sung.
