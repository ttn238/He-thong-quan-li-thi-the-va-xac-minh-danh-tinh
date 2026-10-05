# UC07 – Bàn Giao Thi Thể & Đóng Vụ Việc (Handover)

## 1. Thông tin chung
- **Mã Use Case:** UC07
- **Tên Use Case:** Bàn giao thi thể cho thân nhân & Đóng vụ việc
- **Actor chính:** Cán bộ xác minh
- **Actor phụ:** Thân nhân người đã xác định danh tính

## 2. Quy trình xử lý
1. Sau khi danh tính đã được phê duyệt chính thức ở `UC06`, cán bộ tiến hành lập hồ sơ bàn giao.
2. Cán bộ mở màn hình "Thủ tục bàn giao":
   - Nhập thông tin người nhận bàn giao (Họ tên, số CCCD, quan hệ nhân thân, địa chỉ thường trú, SĐT liên lạc).
   - Liệt kê toàn bộ đồ vật, tư trang kèm theo được trao trả.
   - Ghi nhận ngày giờ và địa điểm thực hiện bàn giao.
   - Tải lên tệp quét Biên bản bàn giao có chữ ký xác nhận của cán bộ thụ lý và thân nhân.
3. Hệ thống lưu thông tin vào bảng `Handover`.
4. Trạng thái vụ việc chuyển sang "Hoàn tất".
5. Hệ thống gửi thông báo xác nhận hoàn tất thủ tục bàn giao cho thân nhân.
6. Cập nhật số liệu thống kê Dashboard và lưu nhật ký hệ thống.
