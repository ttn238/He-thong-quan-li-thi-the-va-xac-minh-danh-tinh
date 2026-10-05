# UC01 – Đăng Ký / Đăng Nhập

## 1. Thông tin chung
- **Mã Use Case:** UC01
- **Tên Use Case:** Đăng ký / Đăng nhập
- **Actor chính:** Người dùng, Cán bộ xác minh, Admin
- **Mục tiêu:** Cho phép tạo tài khoản (đối với người dùng) và xác thực để truy cập các chức năng theo đúng vai trò.

## 2. Điều kiện
- **Điều kiện tiên quyết:** Thiết bị có kết nối mạng tới hệ thống. Với đăng nhập: đã có tài khoản hợp lệ.
- **Điều kiện sau:** Đăng nhập thành công, hệ thống tạo phiên làm việc (JWT Token) và chuyển đến giao diện tương ứng với vai trò.

## 3. Luồng sự kiện chính (Đăng nhập)
1. Actor mở trang đăng nhập trên trình duyệt.
2. Hệ thống hiển thị biểu mẫu đăng nhập (Tên đăng nhập / Email và Mật khẩu).
3. Actor nhập thông tin xác thực và nhấn nút "Đăng nhập".
4. Hệ thống kiểm tra tính hợp lệ của tài khoản và đối chiếu mật khẩu đã băm.
5. Hệ thống xác định vai trò của Actor (Admin / Officer / User) và chuyển hướng tới giao diện tương ứng.
6. Hệ thống ghi nhật ký đăng nhập thành công vào `NhatKyHeThong`.

## 4. Luồng rẽ nhánh
- **1a. Đăng ký tài khoản (Dành riêng cho Người dùng):**
  - Người dùng chọn "Đăng ký tài khoản mới".
  - Nhập họ tên, email, số điện thoại, CCCD (nếu có), mật khẩu và xác nhận mật khẩu.
  - Hệ thống kiểm tra định dạng và tính duy nhất của email/SĐT/CCCD.
  - Hệ thống tạo tài khoản mới với vai trò mặc định `User` và chuyển sang trang đăng nhập.
  *(Lưu ý: Tài khoản Cán bộ xác minh và Admin do Admin cấp trực tiếp, không cho phép tự đăng ký).*

## 5. Luồng ngoại lệ
- **4a. Thông tin xác thực không chính xác:** Hệ thống báo lỗi sai tên đăng nhập hoặc mật khẩu. Nếu nhập sai liên tiếp quá số lần quy định (ví dụ: 5 lần), tài khoản bị tạm khóa trong 15 phút.
- **4b. Tài khoản đang bị khóa:** Hệ thống từ chối đăng nhập và hiển thị thông báo liên hệ quản trị viên.

## 6. Yêu cầu đặc biệt
- Mật khẩu phải được băm an toàn (BCrypt/Argon2).
- Mọi lượt đăng nhập thành công hoặc thất bại đều phải được ghi log kèm IP và User-Agent.
