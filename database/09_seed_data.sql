USE QL_XACMINHTHITHE;
GO

/* 1. Vai tro */
DECLARE @VaiTro TABLE (Ma VARCHAR(20) PRIMARY KEY, Ten NVARCHAR(100));
INSERT @VaiTro (Ma, Ten) VALUES
 ('ADMIN',         N'Quản trị hệ thống'),
 ('CONG_AN',       N'Công an / cán bộ xác minh'),
 ('CONG_AN_DUYET', N'Công an có thẩm quyền duyệt'),
 ('NHA_XAC',       N'Nhân viên nhà xác'),
 ('NGUOI_DAN',     N'Người dân / người thân');

INSERT dbo.VaiTro (TenVaiTro) SELECT Ten FROM @VaiTro;

/* 2. Quyen (resource x action) */
INSERT dbo.QuyenHeThong (Resource, [Action]) VALUES
 ('ACCOUNT','VIEW'),('ACCOUNT','CREATE'),('ACCOUNT','UPDATE'),
 ('PERMISSION','VIEW'),('PERMISSION','UPDATE'),
 ('CATALOG','VIEW'),('CATALOG','CREATE'),('CATALOG','UPDATE'),
 ('INCIDENT','VIEW'),('INCIDENT','CREATE'),('INCIDENT','UPDATE'),('INCIDENT','CONFIRM'),
 ('FILE','VIEW'),('FILE','CREATE'),('FILE','UPDATE'),('FILE','DELETE'),
 ('AI_RESULT','VIEW'),('AI_RESULT','CONFIRM'),
 ('MISSING_PERSON','VIEW'),('MISSING_PERSON','CREATE'),('MISSING_PERSON','UPDATE'),('MISSING_PERSON','CONFIRM'),
 ('BODY','VIEW'),('BODY','CREATE'),('BODY','UPDATE'),('BODY','DELETE'),
 ('STORAGE','VIEW'),('STORAGE','CREATE'),('STORAGE','UPDATE'),
 ('OBJECT','VIEW'),('OBJECT','CREATE'),('OBJECT','UPDATE'),
 ('MATCHING','VIEW'),('MATCHING','CREATE'),('MATCHING','UPDATE'),
 ('VERIFICATION','VIEW'),('VERIFICATION','CREATE'),('VERIFICATION','UPDATE'),
 ('VERIFICATION','CONFIRM'),('VERIFICATION','APPROVE'),
 ('HANDOVER','VIEW'),('HANDOVER','CREATE'),('HANDOVER','UPDATE'),('HANDOVER','CONFIRM'),('HANDOVER','HANDOVER'),
 ('REPORT','VIEW'),('REPORT','EXPORT'),
 ('AUDIT','VIEW');

/* 3. Ma tran phan quyen (PhamVi: ALL / OWN = chi du lieu cua minh / PARTIAL = mot phan) */
INSERT dbo.VaiTro_Quyen (MaVaiTro, MaQuyen, PhamVi)
SELECT r.MaVaiTro, q.MaQuyen, v.PhamVi
FROM (VALUES
 -- ADMIN
 ('ADMIN','ACCOUNT','VIEW','ALL'),('ADMIN','ACCOUNT','CREATE','ALL'),('ADMIN','ACCOUNT','UPDATE','ALL'),
 ('ADMIN','PERMISSION','VIEW','ALL'),('ADMIN','PERMISSION','UPDATE','ALL'),
 ('ADMIN','CATALOG','VIEW','ALL'),('ADMIN','CATALOG','CREATE','ALL'),('ADMIN','CATALOG','UPDATE','ALL'),
 ('ADMIN','INCIDENT','VIEW','ALL'),('ADMIN','FILE','VIEW','ALL'),('ADMIN','AI_RESULT','VIEW','ALL'),
 ('ADMIN','BODY','VIEW','ALL'),('ADMIN','BODY','DELETE','ALL'),
 ('ADMIN','STORAGE','VIEW','ALL'),('ADMIN','STORAGE','CREATE','ALL'),('ADMIN','STORAGE','UPDATE','ALL'),
 ('ADMIN','MATCHING','VIEW','ALL'),('ADMIN','MATCHING','UPDATE','ALL'),
 ('ADMIN','VERIFICATION','VIEW','ALL'),
 ('ADMIN','REPORT','VIEW','ALL'),('ADMIN','REPORT','EXPORT','ALL'),('ADMIN','AUDIT','VIEW','ALL'),
 -- CONG_AN (khong co APPROVE; quyen duyet chi co o vai tro CONG_AN_DUYET)
 ('CONG_AN','INCIDENT','VIEW','ALL'),('CONG_AN','INCIDENT','UPDATE','ALL'),('CONG_AN','INCIDENT','CONFIRM','ALL'),
 ('CONG_AN','FILE','VIEW','ALL'),('CONG_AN','FILE','CREATE','ALL'),('CONG_AN','FILE','UPDATE','ALL'),
 ('CONG_AN','AI_RESULT','VIEW','ALL'),('CONG_AN','AI_RESULT','CONFIRM','ALL'),
 ('CONG_AN','MISSING_PERSON','VIEW','ALL'),('CONG_AN','MISSING_PERSON','CREATE','ALL'),
 ('CONG_AN','MISSING_PERSON','UPDATE','ALL'),('CONG_AN','MISSING_PERSON','CONFIRM','ALL'),
 ('CONG_AN','BODY','VIEW','ALL'),('CONG_AN','BODY','CREATE','ALL'),('CONG_AN','BODY','UPDATE','ALL'),
 ('CONG_AN','STORAGE','VIEW','ALL'),('CONG_AN','STORAGE','UPDATE','ALL'),
 ('CONG_AN','OBJECT','VIEW','ALL'),('CONG_AN','OBJECT','CREATE','ALL'),('CONG_AN','OBJECT','UPDATE','ALL'),
 ('CONG_AN','MATCHING','VIEW','ALL'),('CONG_AN','MATCHING','CREATE','ALL'),
 ('CONG_AN','VERIFICATION','VIEW','ALL'),('CONG_AN','VERIFICATION','CREATE','ALL'),
 ('CONG_AN','VERIFICATION','UPDATE','ALL'),('CONG_AN','VERIFICATION','CONFIRM','ALL'),
 ('CONG_AN','HANDOVER','VIEW','ALL'),('CONG_AN','HANDOVER','CREATE','ALL'),
 ('CONG_AN','HANDOVER','UPDATE','ALL'),('CONG_AN','HANDOVER','CONFIRM','ALL'),
 ('CONG_AN','REPORT','VIEW','ALL'),('CONG_AN','REPORT','EXPORT','ALL'),('CONG_AN','AUDIT','VIEW','ALL'),
 -- NHA_XAC (mot phan)
 ('NHA_XAC','BODY','VIEW','PARTIAL'),('NHA_XAC','BODY','UPDATE','PARTIAL'),
 ('NHA_XAC','STORAGE','VIEW','ALL'),('NHA_XAC','STORAGE','CREATE','ALL'),('NHA_XAC','STORAGE','UPDATE','ALL'),
 ('NHA_XAC','OBJECT','VIEW','ALL'),('NHA_XAC','OBJECT','CREATE','ALL'),('NHA_XAC','OBJECT','UPDATE','ALL'),
 ('NHA_XAC','FILE','VIEW','PARTIAL'),('NHA_XAC','FILE','CREATE','PARTIAL'),
 ('NHA_XAC','HANDOVER','VIEW','ALL'),('NHA_XAC','HANDOVER','UPDATE','ALL'),
 ('NHA_XAC','HANDOVER','CONFIRM','ALL'),('NHA_XAC','HANDOVER','HANDOVER','ALL'),
 ('NHA_XAC','REPORT','VIEW','PARTIAL'),('NHA_XAC','REPORT','EXPORT','PARTIAL'),('NHA_XAC','AUDIT','VIEW','PARTIAL'),
 -- NGUOI_DAN (chi du lieu cua minh)
 ('NGUOI_DAN','INCIDENT','VIEW','OWN'),('NGUOI_DAN','INCIDENT','CREATE','OWN'),('NGUOI_DAN','INCIDENT','UPDATE','OWN'),
 ('NGUOI_DAN','FILE','VIEW','OWN'),('NGUOI_DAN','FILE','CREATE','OWN'),
 ('NGUOI_DAN','MISSING_PERSON','VIEW','OWN'),('NGUOI_DAN','MISSING_PERSON','CREATE','OWN'),
 ('NGUOI_DAN','MISSING_PERSON','UPDATE','OWN')
) AS v (Ma, Resource, [Action], PhamVi)
JOIN @VaiTro m          ON m.Ma = v.Ma
JOIN dbo.VaiTro r       ON r.TenVaiTro = m.Ten
JOIN dbo.QuyenHeThong q ON q.Resource = v.Resource AND q.[Action] = v.[Action];

-- CONG_AN_DUYET = toan bo quyen cua CONG_AN + quyen duyet ket luan danh tinh
DECLARE @CongAn INT, @CongAnDuyet INT;
SELECT @CongAn      = r.MaVaiTro FROM dbo.VaiTro r JOIN @VaiTro m ON m.Ten = r.TenVaiTro WHERE m.Ma = 'CONG_AN';
SELECT @CongAnDuyet = r.MaVaiTro FROM dbo.VaiTro r JOIN @VaiTro m ON m.Ten = r.TenVaiTro WHERE m.Ma = 'CONG_AN_DUYET';

INSERT dbo.VaiTro_Quyen (MaVaiTro, MaQuyen, PhamVi)
SELECT @CongAnDuyet, MaQuyen, PhamVi FROM dbo.VaiTro_Quyen WHERE MaVaiTro = @CongAn;

INSERT dbo.VaiTro_Quyen (MaVaiTro, MaQuyen, PhamVi)
SELECT @CongAnDuyet, q.MaQuyen, 'ALL'
FROM dbo.QuyenHeThong q WHERE q.Resource = 'VERIFICATION' AND q.[Action] = 'APPROVE';

/* 4. Tham so he thong (INSERT 1 cau de tong trong so = 100 ngay luc ket thuc cau lenh) */
INSERT dbo.ThamSoHeThong (TenThamSo, GiaTri, MoTa) VALUES
 ('MATCH_W_BASIC',       N'50',  N'Trọng số: giới tính, tuổi'),
 ('MATCH_W_LOCATION',    N'30',  N'Trọng số: địa điểm'),
 ('MATCH_W_TIME',        N'20',  N'Trọng số: thời gian'),
 ('MATCH_T_POSSIBLE',    N'50',  N'Điểm > ngưỡng này: có khả năng'),
 ('MATCH_T_VERIFY',      N'75',  N'Điểm > ngưỡng này: cần xác minh'),
 ('MATCH_T_PRIORITY',    N'90',  N'Điểm > ngưỡng này: ưu tiên xác minh'),
 ('UPLOAD_MAX_MB',       N'10',  N'Dung lượng tối đa mỗi tệp (MB)'),
 ('OCR_MIN_CONFIDENCE',  N'60',  N'Độ tin cậy OCR tối thiểu (%) để tự điền dữ liệu'),
 ('REVIEW_INTERVAL_DAYS',N'30',  N'Chu kỳ nhắc rà soát hồ sơ chưa xác định (ngày)');

/* 5. Danh muc */
INSERT dbo.LoaiTinBao (TenLoai) VALUES
 (N'Tai nạn giao thông'), (N'Phát hiện người tử vong'), (N'Đuối nước'), (N'Cháy nổ'), (N'Khác');

INSERT dbo.LoaiDoVat (TenLoai) VALUES
 (N'Điện thoại'), (N'Ví / bóp'), (N'Giấy tờ tùy thân'), (N'Chìa khóa'),
 (N'Quần áo'), (N'Trang sức'), (N'Khác');

/* 6. Cau truc luu tru mau: Khu A -> Tu A01 (4 ngan), Tu A02 (2 ngan) */
INSERT dbo.ViTriLuuTru (Khu, Tu, Ngan, SucChua) VALUES
 (N'A', N'A01', N'N01', 1), (N'A', N'A01', N'N02', 1), (N'A', N'A01', N'N03', 1), (N'A', N'A01', N'N04', 1),
 (N'A', N'A02', N'N01', 1), (N'A', N'A02', N'N02', 1);

/* 7. Tai khoan mau (MatKhauHash la CHO GIU CHO - thay bang bcrypt/argon2 that khi chay ung dung) */
INSERT dbo.TaiKhoanNguoiDung (TenDangNhap, Email, SoDienThoai, MatKhauHash, HoTen, DonVi, MaVaiTro)
SELECT v.TenDangNhap, v.Email, v.SDT, 'CHANGE_ME_HASH', v.HoTen, v.DonVi, r.MaVaiTro
FROM (VALUES
 ('admin',    'admin@demo.local',    '0900000001', N'Quản trị viên',       N'Phòng Công nghệ thông tin', 'ADMIN'),
 ('canbo01',  'canbo01@demo.local',  '0900000002', N'Cán bộ Nguyễn Văn A', N'Công an Quận 1',            'CONG_AN'),
 ('canbo02',  'canbo02@demo.local',  '0900000003', N'Cán bộ Trần Thị B',   N'Công an Quận 1',            'CONG_AN_DUYET'),
 ('nhaxac01', 'nhaxac01@demo.local', '0900000004', N'Nhân viên Lê Văn C',  N'Nhà xác Trung tâm',         'NHA_XAC'),
 ('dan01',    'dan01@demo.local',    '0900000005', N'Phạm Thị D',          NULL,                          'NGUOI_DAN')
) AS v (TenDangNhap, Email, SDT, HoTen, DonVi, Ma)
JOIN @VaiTro m    ON m.Ma = v.Ma
JOIN dbo.VaiTro r ON r.TenVaiTro = m.Ten;

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
GO 