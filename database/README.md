# Cơ Sở Dữ Liệu (Database Scripts)

## 1. Tổng quan
Hệ thống sử dụng hệ quản trị CSDL quan hệ (SQL Server / PostgreSQL). Các tập lệnh SQL được phân chia tuần tự theo thứ tự thực thi từ 01 đến 09 để đảm bảo tính toàn vẹn dữ liệu.

## 2. Danh mục kịch bản SQL (01 - 09)
1. `01_create_database.sql`: Khởi tạo cơ sở dữ liệu và cấu hình collation/encoding UTF-8.
2. `02_create_tables.sql`: Định nghĩa các bảng chính:
   - `TaiKhoan`, `NguoiDung`, `CanBo`, `NguoiThan`
   - `HoSoNguoiMatTich`, `HoSoChuaXacDinh`, `HoSoNguoiDaXacDinh`, `VuViec`
   - `HinhAnh`, `KetQuaAI`, `AI_Matching`, `YeuCauXacNhan`, `ThongBao`, `NhatKyHeThong`
3. `03_constraints.sql`: Ràng buộc khóa ngoại (Foreign Keys), khóa duy nhất (Unique), kiểm tra logic (Check Constraints).
4. `04_indexes.sql`: Thiết lập chỉ mục (Indexes) trên các cột tìm kiếm thường xuyên (CCCD, Họ tên, Ngày mất tích, Khu vực, Trạng thái, Điểm matching).
5. `05_views.sql`: Khung nhìn phục vụ tra cứu tổng hợp và thống kê Dashboard.
6. `06_procedures.sql`: Thủ tục lưu trữ xử lý các luồng nghiệp vụ phức tạp.
7. `07_functions.sql`: Hàm tính toán tiện ích (tính tuổi, chuẩn hóa chuỗi, tính điểm trọng số cơ bản).
8. `08_triggers.sql`: Tự động ghi nhật ký hệ thống (`NhatKyHeThong`) khi có thay đổi trạng thái hồ sơ.
9. `09_seed_data.sql`: Dữ liệu mẫu (Tài khoản Admin mặc định, các vai trò, dữ liệu mẫu ban đầu phục vụ kiểm thử).
