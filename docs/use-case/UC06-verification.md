# UC06 – Thẩm Tra & Xác Minh Danh Tính (Verification)

## 1. Thông tin chung
- **Mã Use Case:** UC06 (bao gồm UC10, UC11)
- **Tên Use Case:** Thẩm tra & Xác minh danh tính
- **Actor chính:** Cán bộ xác minh
- **Mục tiêu:** Cán bộ có thẩm quyền trực tiếp thẩm tra tài liệu, đối chiếu kết quả đề xuất từ AI với minh chứng thực tế và đưa ra kết luận pháp lý chính thức.

## 2. Quy trình xử lý
1. Cán bộ đăng nhập vào trang quản trị cán bộ, truy cập danh sách "Yêu cầu xác nhận nhân thân" cần xử lý.
2. Cán bộ chọn một yêu cầu cụ thể:
   - Xem thông tin hồ sơ người mất tích và hồ sơ tử thi tương ứng.
   - Xem bảng điểm phân rã của AI (Module Matching) và hình ảnh so khớp đối chiếu.
   - Kiểm tra các tài liệu minh chứng do người thân tải lên (giấy khai sinh, CCCD, kết quả xét nghiệm ADN, giấy tờ quan hệ...).
3. Cán bộ liên hệ xác minh thực tế / kiểm tra thực địa / giám định pháp y nếu cần thiết.
4. **Đưa ra kết luận chính thức:**
   - **Trường hợp 1 - Xác nhận (Approved):**
     - Cán bộ chọn "Xác nhận danh tính".
     - Chuyển trạng thái hồ sơ sang `HoSoNguoiDaXacDinh`.
     - Cập nhật trạng thái vụ việc.
     - Hệ thống phát thông báo kết quả cho người thân và chuẩn bị thủ tục bàn giao.
   - **Trường hợp 2 - Từ chối (Rejected):**
     - Cán bộ chọn "Từ chối".
     - Bắt buộc nhập lý do từ chối (minh chứng không khớp, kết quả ADN không trùng...).
     - Cập nhật trạng thái yêu cầu sang "Từ chối".
     - Hệ thống phát thông báo kèm lý do cho người thân.
   - **Trường hợp 3 - Yêu cầu bổ sung (Requires Additional Info):**
     - Cán bộ yêu cầu người thân nộp thêm giấy tờ, hồ sơ chuyển sang trạng thái "Yêu cầu bổ sung".
5. Ghi vết toàn bộ hành vi và lý do vào `NhatKyHeThong`.
