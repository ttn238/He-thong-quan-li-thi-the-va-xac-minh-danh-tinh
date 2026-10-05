# UC03 – Đăng Ký Tìm Người Mất Tích

## 1. Thông tin chung
- **Mã Use Case:** UC03
- **Tên Use Case:** Đăng ký tìm người mất tích
- **Actor chính:** Người dùng / Thân nhân
- **Actor phụ:** Hệ thống AI (kích hoạt đối chiếu tự động)
- **Mục tiêu:** Cho phép người dùng đăng ký hồ sơ tìm kiếm người thân bị mất tích, cung cấp chi tiết nhân dạng và hoàn cảnh xảy ra vụ việc.

## 2. Quy trình xử lý
1. Người dùng đăng nhập hệ thống và chọn chức năng "Đăng ký tìm người mất tích".
2. Hệ thống hiển thị biểu mẫu khai báo:
   - **Thông tin cơ bản:** Họ tên, giới tính, ngày sinh/độ tuổi, quê quán, địa chỉ cư trú cuối cùng.
   - **Đặc điểm nhận dạng:** Chiều cao, cân nặng, màu tóc, vết sẹo, nốt ruồi, dị tật, hình xăm, giọng nói, trang phục lúc rời đi.
   - **Hoàn cảnh mất tích:** Ngày giờ mất tích, địa điểm cuối cùng nhìn thấy, phương tiện đi lại (kèm biển số nếu có).
   - **Hình ảnh:** Tải lên một hoặc nhiều ảnh chân dung rõ mặt của người mất tích.
3. Người dùng nhấn nút "Gửi hồ sơ".
4. Hệ thống kiểm tra dữ liệu:
   - Nếu thiếu thông tin bắt buộc $\to$ hiển thị thông báo yêu cầu bổ sung.
   - Nếu hợp lệ $\to$ tạo bản ghi `HoSoNguoiMatTich` và lưu các tệp ảnh vào bảng `HinhAnh`.
5. Hệ thống tự động đẩy dữ liệu sang **Phân hệ AI** để bắt đầu quy trình đối chiếu và phân tích khuôn mặt với toàn bộ cơ sở dữ liệu thi thể hiện có (kích hoạt UC17 & UC18).
6. Hệ thống hiển thị mã hồ sơ tra cứu và gửi thông báo xác nhận đã tạo hồ sơ thành công cho người dùng.
