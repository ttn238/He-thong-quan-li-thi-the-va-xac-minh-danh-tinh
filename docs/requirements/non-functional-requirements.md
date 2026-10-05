# Đặc Tả Yêu Cầu Phi Chức Năng (Non-Functional Requirements)

## 1. Yêu cầu về Bảo mật & Quyền riêng tư (Security & Privacy)
- **NFR1.1 (Mã hóa mật khẩu):** Mật khẩu người dùng và cán bộ phải được băm an toàn (BCrypt / Argon2 / PBKDF2) kèm muối (salt), tuyệt đối không lưu dạng plain-text.
- **NFR1.2 (Bảo vệ hình ảnh nhạy cảm):** Hình ảnh tử thi (`BodyImage`) phải được lưu trữ trong phân vùng an toàn (Private Storage / Presigned URL có thời hạn). Tuyệt đối không cho phép truy cập public hay qua đường dẫn trực tiếp.
- **NFR1.3 (Kiểm soát truy cập dựa trên vai trò - RBAC):** Hệ thống phân quyền chặt chẽ giữa Quản trị viên (Admin), Cán bộ xác minh (Officer) và Người dân (User). Người dân chỉ xem được thông tin hồ sơ của chính mình hoặc thông tin hồ sơ công khai đã được kiểm duyệt.
- **NFR1.4 (Ghi vết kiểm toán - Audit Trail):** Mọi hành động đăng nhập, tra cứu hồ sơ tử thi, thay đổi trạng thái xác minh, phê duyệt/từ chối đều phải ghi lại IP, thời gian, tài khoản thực hiện vào bảng `NhatKyHeThong`.

## 2. Yêu cầu về Hiệu năng (Performance)
- **NFR2.1 (Thời gian phản hồi API):** Các thao tác tra cứu, hiển thị danh sách hồ sơ thông thường phản hồi dưới 1 giây với 95% số lượng request.
- **NFR2.2 (Thời gian xử lý AI):**
  - So khớp ảnh đơn lẻ (1:1): dưới 2 giây.
  - Quét đối chiếu AI Matching toàn bộ cơ sở dữ liệu (1:N): chạy dưới dạng Background Job không gây nghẽn API chính.
- **NFR2.3 (Khả năng chịu tải):** Hệ thống hỗ trợ tối thiểu 500 người dùng đồng thời (concurrent users) mà không bị suy giảm hiệu năng đáng kể.

## 3. Yêu cầu về Tính khả dụng & Tin cậy (Availability & Reliability)
- **NFR3.1 (Thời gian hoạt động):** Đạt mức sẵn sàng 99.5% (Uptime), hỗ trợ backup dữ liệu tự động hàng ngày.
- **NFR3.2 (Xử lý lỗi & Phục hồi):** Hệ thống có middleware bắt ngoại lệ tập trung (Exception Handling), trả về mã lỗi và thông báo thân thiện, không lộ cấu trúc mã nguồn hoặc stack trace cho client.

## 4. Yêu cầu về Tính mở rộng & Tương thích (Scalability & Compatibility)
- **NFR4.1 (Kiến trúc phân tách):** Frontend, Backend API và AI Service hoạt động độc lập, có thể scale riêng lẻ (đặc biệt là AI Service có thể bổ sung GPU worker).
- **NFR4.2 (Đa nền tảng thiết bị):** Giao diện web tương thích trên các trình duyệt hiện đại (Chrome, Edge, Firefox, Safari) và hiển thị tốt trên thiết bị di động (Responsive Design).
