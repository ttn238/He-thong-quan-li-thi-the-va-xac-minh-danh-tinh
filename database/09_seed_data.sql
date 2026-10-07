-- HO SO NGUOI MAT TICH --
INSERT INTO HoSoNguoiMatTich (MaNguoiDung, HoTen, GioiTinh, Tuoi, ChieuCao, DacDiemNhanDang, Seo, HinhXam,
                              TrangPhuc, ThoiGianMatTich, DiaDiemCuoiThay, QuanHeVoiNguoiKhai, SoLienHe) VALUES
                             (1, N'Nguyễn Văn Bảo', N'Nam', 35, 170, N'Da ngăm, tóc ngắn', N'Sẹo dài 5cm ở cẳng tay trái', NULL,
                              N'Áo thun xanh, quần jean', '2026-09-20 18:00', N'Đường Xuân La, Hà Nội', N'Anh em', '0912345678'),
                             (2, N'Trần Văn Cường', N'Nam', 60, 165, N'Hói đầu, đeo kính', NULL, N'Hình xăm rồng ở vai phải',
                              N'Áo sơ mi trắng', '2026-09-25 08:00', N'Chợ Bến Thành, TP.HCM', N'Con gái', '0987654321');

-- HO SO THI THE -- 
INSERT INTO HoSoThiThe (MaTinBao, ThoiGianPhatHien, DiaDiem, GioiTinh, TuoiUocTinh, ChieuCao, DacDiemNhanDang,
                        Seo, HinhXam, TrangPhuc, TinhTrang, MaViTri, TrangThai, MaCanBoLap) VALUES
                        (1, '2026-09-20 22:00', N'Đường Xuân La, Hà Nội', N'Nam', 35, 170, N'Da ngăm, tóc ngắn',
                        N'Sẹo dài khoảng 5cm ở cẳng tay trái', NULL, N'Áo thun xanh, quần jean', N'Đã bảo quản', 1, N'DangXacMinh', 1),
                        (NULL, '2026-09-28 07:00', N'Kênh Nhiêu Lộc, TP.HCM', N'Nam', 58, 164, N'Hói đầu',
                         NULL, N'Hình xăm rồng ở vai phải', N'Áo sơ mi trắng', N'Đã bảo quản', 2, N'ChuaXacDinh', 2);
-- THONG BAO --
INSERT INTO ThongBao (MaTaiKhoan, TieuDe, NoiDung) VALUES
                         (2, N'Hồ sơ nghi trùng',    N'Hồ sơ mất tích #1 nghi trùng với HS001 (tổng điểm cao). Vui lòng kiểm tra.'),
                         (5, N'Tin báo đã tiếp nhận', N'Tin báo của bạn đã được cán bộ tiếp nhận và đang xác minh.');

-- NHAT KY HE THONG --
INSERT INTO NhatKyHeThong (MaTaiKhoan, HanhDong, BangTacDong, MaDoiTuong, ChiTiet) VALUES
                         (1, N'DangNhap', NULL,        NULL, N'Admin đăng nhập'),
                         (2, N'Them',     N'HoSoThiThe', 1,  N'Lập hồ sơ thi thể HS001 từ tin báo 1'),
                         (4, N'Them',     N'TiepNhanThiThe', 1, N'Tiếp nhận thi thể HS001 vào Khu A / Tủ A01 / Ngăn 01');