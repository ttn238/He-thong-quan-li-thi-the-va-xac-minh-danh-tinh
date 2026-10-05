# Đặc Tả Yêu Cầu Chức Năng (Functional Requirements)

## 1. Nhóm chức năng Quản lý Tài khoản & Phân quyền (FR1)
- **FR1.1 (Đăng ký tài khoản User):** Cho phép người dân tự đăng ký tài khoản với họ tên, email, SĐT, CCCD và mật khẩu.
- **FR1.2 (Cấp tài khoản Cán bộ & Admin):** Tài khoản cán bộ xác minh và quản trị viên do Admin cấp, không cho phép tự đăng ký tự do.
- **FR1.3 (Đăng nhập & Phân quyền RBAC):** Xác thực tài khoản, kiểm tra vai trò (Admin, Officer, User) và chuyển hướng tới giao diện chức năng tương ứng.
- **FR1.4 (Khóa tài khoản tạm thời):** Tự động khóa tạm thời khi nhập sai mật khẩu quá số lần quy định.
- **FR1.5 (Quản lý hồ sơ cá nhân):** Đổi mật khẩu, cập nhật thông tin cá nhân và xác thực CCCD/SĐT.

## 2. Nhóm chức năng Khai báo Người thân & Đăng ký tìm kiếm (FR2)
- **FR2.1 (Khai báo người thân):** Người dùng thêm, sửa, xóa thông tin người thân (họ tên, ngày sinh, CCCD, SĐT, quan hệ, đặc điểm nhận dạng).
- **FR2.2 (Đăng ký tìm người mất tích):** Tiếp nhận thông tin người mất tích, đặc điểm nhận dạng, hoàn cảnh mất tích và tải ảnh đính kèm.
- **FR2.3 (Kiểm tra dữ liệu đầu vào):** Bắt buộc điền đủ các trường tối thiểu, yêu cầu bổ sung nếu thông tin chưa hợp lệ trước khi lưu hồ sơ.

## 3. Nhóm chức năng Tiếp nhận & Quản lý Hồ sơ Tử thi (FR3)
- **FR3.1 (Tiếp nhận hồ sơ tử thi chưa xác định):** Cán bộ nhập thông tin vụ việc, hiện trường, đặc điểm nhận dạng, đồ vật/tư trang và tải ảnh tử thi vào hệ thống lưu trữ bảo mật.
- **FR3.2 (Quản lý đồ vật/tư trang):** Lưu trữ danh mục đồ vật, quần áo, trang sức, phương tiện thu được tại hiện trường.
- **FR3.3 (Tự động kích hoạt AI):** Khi hồ sơ mới được tạo, hệ thống tự động đẩy dữ liệu sang phân hệ AI để đối chiếu toàn hệ thống.

## 4. Nhóm chức năng Trí tuệ nhân tạo (AI Engine) (FR4)
- **FR4.1 (Nhận diện biển số xe):** OCR ký tự biển số từ ảnh hiện trường tai nạn, phục vụ điều tra nguồn gốc xe.
- **FR4.2 (Phân tích ảnh):** Phát hiện người, phương tiện, đặc điểm nhân dạng nổi bật từ hình ảnh.
- **FR4.3 (So khớp khuôn mặt):** Trích xuất vector đặc trưng và tính độ tương đồng giữa ảnh người mất tích và ảnh thi thể.
- **FR4.4 (AI Matching hồ sơ):** Chấm điểm đối chiếu 5 tiêu chí:
  - Thông tin cá nhân (30%)
  - Hình ảnh (25%)
  - Đặc điểm nhận dạng (20%)
  - Địa điểm (15%)
  - Thời gian (10%)
- **FR4.5 (Phân loại ngưỡng đề xuất):**
  - `> 50%`: Có khả năng trùng khớp.
  - `> 75%`: Cần xác minh.
  - `> 90%`: Ưu tiên xác minh khẩn.
  *(AI chỉ mang tính đề xuất, không tự động kết luận danh tính).*

## 5. Nhóm chức năng Yêu cầu Xác nhận & Thẩm tra (FR5)
- **FR5.1 (Gửi yêu cầu xác nhận):** Người dùng chọn hồ sơ nghi trùng, tải lên minh chứng nhân thân (giấy khai sinh, hộ khẩu, ADN...) và gửi yêu cầu.
- **FR5.2 (Thẩm tra hồ sơ):** Cán bộ xem xét danh sách yêu cầu, đối chiếu gợi ý AI và kiểm tra giấy tờ thực tế.
- **FR5.3 (Kết luận chính thức):** Cán bộ đưa ra kết luận: "Đã xác nhận" hoặc "Từ chối" (bắt buộc nhập lý do).
- **FR5.4 (Theo dõi trạng thái):** Cung cấp tiến trình thời gian thực cho người dùng: *Đã gửi $\to$ Đang xác minh $\to$ Yêu cầu bổ sung $\to$ Đã xác nhận / Từ chối $\to$ Hoàn tất*.
- **FR5.5 (Bàn giao thi thể):** Lập biên bản và cập nhật thông tin bàn giao khi đã xác nhận danh tính chính thức.

## 6. Nhóm chức năng Thông báo tự động (FR6)
- **FR6.1 (Thông báo nghi trùng khớp):** Tự động phát thông báo khi AI tìm thấy hồ sơ có độ tương đồng vượt ngưỡng quy định.
- **FR6.2 (Thông báo tiến trình xác minh):** Cập nhật kết quả phê duyệt, yêu cầu bổ sung giấy tờ tới người thân.
- **FR6.3 (Nguyên tắc bảo mật nội dung thông báo):** Không đính kèm ảnh nhạy cảm hoặc thông tin tử thi chi tiết, chỉ gửi mã hồ sơ và hướng dẫn liên hệ.

## 7. Nhóm chức năng Quản trị & Báo cáo Thống kê (FR7)
- **FR7.1 (Dashboard điều hành):** Hiển thị thống kê hồ sơ theo tháng, theo khu vực, tỷ lệ xác minh thành công.
- **FR7.2 (Xuất báo cáo):** Hỗ trợ kết xuất báo cáo thống kê định dạng PDF và Excel.
- **FR7.3 (Nhật ký hệ thống - Audit Log):** Ghi vết chi tiết mọi thao tác tạo, sửa, xóa, duyệt và đăng nhập của người dùng và cán bộ.
