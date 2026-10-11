USE QL_XACMINHTHITHE;
GO
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

/* =========================================================================
   09_seed_data.sql - DỮ LIỆU KHỞI TẠO VÀ DỮ LIỆU MẪU KIỂM THỬ HỆ THỐNG
   Bao gồm:
     1. Danh mục Vai trò (Roles)
     2. Danh mục Quyền hệ thống (Permissions)
     3. Ma trận Phân quyền (RBAC Matrix)
     4. Tham số hệ thống (System Settings)
     5. Danh mục loại tin báo, loại đồ vật
     6. Vị trí lưu trữ tử thi
     7. Danh sách Tài khoản mẫu (Admin, Cán bộ, Nhà xác, Người dân)
     8. Hồ sơ mẫu Người mất tích, Tử thi, Thân nhân, Đồ vật
     9. Dữ liệu mẫu Hình ảnh (HinhAnh - Phần 3.7)
    10. Lịch sử đăng nhập, Thông báo và Nhật ký hệ thống mẫu
   ========================================================================= */

/* 1. VAI TRÒ */
DECLARE @VaiTro TABLE (Ma VARCHAR(20) PRIMARY KEY, Ten NVARCHAR(100));
INSERT @VaiTro (Ma, Ten) VALUES
 ('ADMIN',         N'Quản trị hệ thống'),
 ('CONG_AN',       N'Công an / cán bộ xác minh'),
 ('CONG_AN_DUYET', N'Công an có thẩm quyền duyệt'),
 ('NHA_XAC',       N'Nhân viên nhà xác'),
 ('NGUOI_DAN',     N'Người dân / người thân');

INSERT dbo.VaiTro (TenVaiTro) 
SELECT Ten FROM @VaiTro
WHERE Ten NOT IN (SELECT TenVaiTro FROM dbo.VaiTro);

/* 2. QUYỀN HỆ THỐNG (Resource x Action) */
DECLARE @Quyen TABLE (Resource VARCHAR(80), [Action] VARCHAR(30));
INSERT @Quyen (Resource, [Action]) VALUES
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

INSERT dbo.QuyenHeThong (Resource, [Action])
SELECT q.Resource, q.[Action] FROM @Quyen q
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.QuyenHeThong existing 
    WHERE existing.Resource = q.Resource AND existing.[Action] = q.[Action]
);

/* 3. MA TRẬN PHÂN QUYỀN (PhamVi: ALL / OWN / PARTIAL) */
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
 -- CONG_AN
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
 -- NHA_XAC
 ('NHA_XAC','BODY','VIEW','PARTIAL'),('NHA_XAC','BODY','UPDATE','PARTIAL'),
 ('NHA_XAC','STORAGE','VIEW','ALL'),('NHA_XAC','STORAGE','CREATE','ALL'),('NHA_XAC','STORAGE','UPDATE','ALL'),
 ('NHA_XAC','OBJECT','VIEW','ALL'),('NHA_XAC','OBJECT','CREATE','ALL'),('NHA_XAC','OBJECT','UPDATE','ALL'),
 ('NHA_XAC','FILE','VIEW','PARTIAL'),('NHA_XAC','FILE','CREATE','PARTIAL'),
 ('NHA_XAC','HANDOVER','VIEW','ALL'),('NHA_XAC','HANDOVER','UPDATE','ALL'),
 ('NHA_XAC','HANDOVER','CONFIRM','ALL'),('NHA_XAC','HANDOVER','HANDOVER','ALL'),
 ('NHA_XAC','REPORT','VIEW','PARTIAL'),('NHA_XAC','REPORT','EXPORT','PARTIAL'),('NHA_XAC','AUDIT','VIEW','PARTIAL'),
 -- NGUOI_DAN
 ('NGUOI_DAN','INCIDENT','VIEW','OWN'),('NGUOI_DAN','INCIDENT','CREATE','OWN'),('NGUOI_DAN','INCIDENT','UPDATE','OWN'),
 ('NGUOI_DAN','FILE','VIEW','OWN'),('NGUOI_DAN','FILE','CREATE','OWN'),
 ('NGUOI_DAN','MISSING_PERSON','VIEW','OWN'),('NGUOI_DAN','MISSING_PERSON','CREATE','OWN'),
 ('NGUOI_DAN','MISSING_PERSON','UPDATE','OWN')
) AS v (Ma, Resource, [Action], PhamVi)
JOIN @VaiTro m          ON m.Ma = v.Ma
JOIN dbo.VaiTro r       ON r.TenVaiTro = m.Ten
JOIN dbo.QuyenHeThong q ON q.Resource = v.Resource AND q.[Action] = v.[Action]
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.VaiTro_Quyen vq 
    WHERE vq.MaVaiTro = r.MaVaiTro AND vq.MaQuyen = q.MaQuyen
);

-- CONG_AN_DUYET = toan bo quyen cua CONG_AN + quyen duyet ket luan danh tinh
DECLARE @CongAn INT, @CongAnDuyet INT;
SELECT @CongAn      = r.MaVaiTro FROM dbo.VaiTro r JOIN @VaiTro m ON m.Ten = r.TenVaiTro WHERE m.Ma = 'CONG_AN';
SELECT @CongAnDuyet = r.MaVaiTro FROM dbo.VaiTro r JOIN @VaiTro m ON m.Ten = r.TenVaiTro WHERE m.Ma = 'CONG_AN_DUYET';

INSERT dbo.VaiTro_Quyen (MaVaiTro, MaQuyen, PhamVi)
SELECT @CongAnDuyet, MaQuyen, PhamVi FROM dbo.VaiTro_Quyen WHERE MaVaiTro = @CongAn
AND MaQuyen NOT IN (SELECT MaQuyen FROM dbo.VaiTro_Quyen WHERE MaVaiTro = @CongAnDuyet);

INSERT dbo.VaiTro_Quyen (MaVaiTro, MaQuyen, PhamVi)
SELECT @CongAnDuyet, q.MaQuyen, 'ALL'
FROM dbo.QuyenHeThong q 
WHERE q.Resource = 'VERIFICATION' AND q.[Action] = 'APPROVE'
AND q.MaQuyen NOT IN (SELECT MaQuyen FROM dbo.VaiTro_Quyen WHERE MaVaiTro = @CongAnDuyet);

/* 4. THAM SỐ HỆ THỐNG */
IF NOT EXISTS (SELECT 1 FROM dbo.ThamSoHeThong)
BEGIN
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
END

/* 5. DANH MỤC */
IF NOT EXISTS (SELECT 1 FROM dbo.LoaiTinBao)
BEGIN
    INSERT dbo.LoaiTinBao (TenLoai) VALUES
     (N'Tai nạn giao thông'), (N'Phát hiện người tử vong'), (N'Đuối nước'), (N'Cháy nổ'), (N'Khác');
END

IF NOT EXISTS (SELECT 1 FROM dbo.LoaiDoVat)
BEGIN
    INSERT dbo.LoaiDoVat (TenLoai) VALUES
     (N'Điện thoại'), (N'Ví / bóp'), (N'Giấy tờ tùy thân'), (N'Chìa khóa'),
     (N'Quần áo'), (N'Trang sức'), (N'Khác');
END

/* 6. VỊ TRÍ LƯU TRỮ */
IF NOT EXISTS (SELECT 1 FROM dbo.ViTriLuuTru)
BEGIN
    INSERT dbo.ViTriLuuTru (Khu, Tu, Ngan, SucChua) VALUES
     (N'A', N'A01', N'N01', 1), (N'A', N'A01', N'N02', 1), (N'A', N'A01', N'N03', 1), (N'A', N'A01', N'N04', 1),
     (N'A', N'A02', N'N01', 1), (N'A', N'A02', N'N02', 1);
END

/* 7. TÀI KHOẢN MẪU (Bao gồm Admin, Cán bộ, Nhà xác, Người dân)
   Mật khẩu mặc định:
     - Admin:    Admin@123
     - Cán bộ:   Canbo@123
     - Nhà xác:  Nhaxac@123
     - Người dân: User@123
   Chuỗi băm chuẩn PBKDF2/HMAC-SHA256 (100000 vòng lặp kèm Salt):
   100000.kh6sXiu8EcrIcTL5NBOzvg==.SwFY+yE3YNgbiVR3IgzeoadrUXTfa1/PY4S/3a2Rmaw=
*/
DECLARE @DefaultHash VARCHAR(255) = '100000.kh6sXiu8EcrIcTL5NBOzvg==.SwFY+yE3YNgbiVR3IgzeoadrUXTfa1/PY4S/3a2Rmaw=';

INSERT dbo.TaiKhoanNguoiDung (TenDangNhap, Email, SoDienThoai, CCCD, MatKhauHash, HoTen, DonVi, ChucVu, MaVaiTro, TrangThai)
SELECT v.TenDangNhap, v.Email, v.SDT, v.CCCD, @DefaultHash, v.HoTen, v.DonVi, v.ChucVu, r.MaVaiTro, N'HoatDong'
FROM (VALUES
 ('admin',    'admin@demo.local',    '0900000001', '001090000001', N'Quản trị viên Hệ thống', N'Phòng Công nghệ thông tin', N'Quản trị viên',       'ADMIN'),
 ('canbo01',  'canbo01@demo.local',  '0900000002', '001090000002', N'Nguyễn Văn An',          N'Công an Quận 1',            N'Cán bộ điều tra',     'CONG_AN'),
 ('canbo02',  'canbo02@demo.local',  '0900000003', '001090000003', N'Trần Thị Bích',          N'Công an Quận 1',            N'Chỉ huy đội xác minh','CONG_AN_DUYET'),
 ('nhaxac01', 'nhaxac01@demo.local', '0900000004', '001090000004', N'Lê Văn Cường',          N'Nhà xác Trung tâm',         N'Cán bộ quản lý kho',  'NHA_XAC'),
 ('dan01',    'dan01@demo.local',    '0900000005', '001090000005', N'Phạm Thị Duyên',         NULL,                          NULL,                   'NGUOI_DAN'),
 ('dan02',    'dan02@demo.local',    '0900000006', '001090000006', N'Hoàng Văn Em',           NULL,                          NULL,                   'NGUOI_DAN')
) AS v (TenDangNhap, Email, SDT, CCCD, HoTen, DonVi, ChucVu, Ma)
JOIN @VaiTro m    ON m.Ma = v.Ma
JOIN dbo.VaiTro r ON r.TenVaiTro = m.Ten
WHERE v.TenDangNhap NOT IN (SELECT TenDangNhap FROM dbo.TaiKhoanNguoiDung);

/* 8. HỒ SƠ MẪU PHỤC VỤ KIỂM THỬ (Khớp 100% cấu trúc bảng) */

-- 8.1. Hồ sơ người mất tích
IF NOT EXISTS (SELECT 1 FROM dbo.HoSoNguoiMatTich)
BEGIN
    INSERT INTO dbo.HoSoNguoiMatTich (HoTen, NgaySinh, GioiTinh, ThoiGianMatTich, NoiMatTich, TrangThai)
    VALUES 
     (N'Nguyễn Văn Bảo', '1991-05-12', N'Nam', '2026-09-20 18:00:00', N'Đường Xuân La, Hà Nội', N'DangTimKiem'),
     (N'Trần Văn Cường', '1966-08-20', N'Nam', '2026-09-25 08:30:00', N'Chợ Bến Thành, Quận 1, TP.HCM', N'DangTimKiem');
END

-- 8.2. Thông tin thân nhân khai báo
IF NOT EXISTS (SELECT 1 FROM dbo.NguoiThan)
BEGIN
    DECLARE @IdMatTich1 BIGINT = (SELECT TOP 1 MaHoSoMatTich FROM dbo.HoSoNguoiMatTich WHERE HoTen = N'Nguyễn Văn Bảo');
    DECLARE @IdMatTich2 BIGINT = (SELECT TOP 1 MaHoSoMatTich FROM dbo.HoSoNguoiMatTich WHERE HoTen = N'Trần Văn Cường');

    IF @IdMatTich1 IS NOT NULL
        INSERT INTO dbo.NguoiThan (HoTen, QuanHe, SoDienThoai, DiaChi, MaHoSoMatTich)
        VALUES (N'Nguyễn Văn Bình', N'Anh trai', '0912345678', N'Số 12 ngõ 45 Xuân La, Hà Nội', @IdMatTich1);

    IF @IdMatTich2 IS NOT NULL
        INSERT INTO dbo.NguoiThan (HoTen, QuanHe, SoDienThoai, DiaChi, MaHoSoMatTich)
        VALUES (N'Trần Thị Mai', N'Con gái', '0987654321', N'123 Lê Lợi, Phường Bến Thành, Quận 1, TP.HCM', @IdMatTich2);
END

-- 8.3. Hồ sơ thi thể chưa xác định
IF NOT EXISTS (SELECT 1 FROM dbo.HoSoThiThe)
BEGIN
    INSERT INTO dbo.HoSoThiThe (ThoiGianPhatHien, DiaDiem, TrangThai, GioiTinh, TuoiUocLuong, ChieuCao, DacDiemCoThe)
    VALUES 
     ('2026-09-20 22:30:00', N'Khu vực chân cầu Nhật Tân, Tây Hồ, Hà Nội', N'DangXacMinh', N'Nam', 35, 170.00, N'Tóc ngắn đen, da ngăm, có vết sẹo dài 5cm ở cẳng tay trái. Trang phục: Áo thun xanh, quần jean dài.'),
     ('2026-09-28 07:15:00', N'Kênh Nhiêu Lộc, đoạn gần cầu Thị Nghè, TP.HCM', N'MoiTiepNhan', N'Nam', 58, 164.00, N'Tóc hoa râm hói đỉnh đầu, có hình xăm rồng ở bả vai phải. Trang phục: Áo sơ mi trắng.');
END

-- 8.4. Đồ vật, tư trang kèm theo thi thể
IF NOT EXISTS (SELECT 1 FROM dbo.DoVatCaNhan)
BEGIN
    DECLARE @IdThiThe1 BIGINT = (SELECT TOP 1 MaHoSoThiThe FROM dbo.HoSoThiThe WHERE GioiTinh = N'Nam' ORDER BY MaHoSoThiThe ASC);
    DECLARE @MaLoaiDT INT = (SELECT TOP 1 MaLoaiDoVat FROM dbo.LoaiDoVat WHERE TenLoai LIKE N'%Điện thoại%');
    DECLARE @MaLoaiTrangSuc INT = (SELECT TOP 1 MaLoaiDoVat FROM dbo.LoaiDoVat WHERE TenLoai LIKE N'%Trang sức%');

    IF @IdThiThe1 IS NOT NULL AND @MaLoaiDT IS NOT NULL
    BEGIN
        INSERT INTO dbo.DoVatCaNhan (Ten, MoTa, TinhTrang, MaHoSoThiThe, MaLoaiDoVat)
        VALUES 
         (N'Điện thoại thông minh', N'Màu đen, màn hình nứt góc trên bên phải, ốp lưng màu xanh', N'Hỏng do ngấm nước', @IdThiThe1, @MaLoaiDT),
         (N'Đồng hồ đeo tay', N'Đồng hồ dây da màu nâu, mặt kim loại', N'Còn hoạt động', @IdThiThe1, ISNULL(@MaLoaiTrangSuc, @MaLoaiDT));
    END
END

/* 9. DỮ LIỆU MẪU HÌNH ẢNH (Phần 3.7 - Bảng HinhAnh) */
IF OBJECT_ID(N'dbo.HinhAnh', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.HinhAnh)
BEGIN
    DECLARE @AccCanBo BIGINT = (SELECT TOP 1 MaTaiKhoan FROM dbo.TaiKhoanNguoiDung WHERE TenDangNhap = 'canbo01');
    DECLARE @AccDan BIGINT = (SELECT TOP 1 MaTaiKhoan FROM dbo.TaiKhoanNguoiDung WHERE TenDangNhap = 'dan01');
    DECLARE @Body1 BIGINT = (SELECT TOP 1 MaHoSoThiThe FROM dbo.HoSoThiThe ORDER BY MaHoSoThiThe ASC);
    DECLARE @Missing1 BIGINT = (SELECT TOP 1 MaHoSoMatTich FROM dbo.HoSoNguoiMatTich ORDER BY MaHoSoMatTich ASC);

    IF @AccCanBo IS NOT NULL AND @Body1 IS NOT NULL
    BEGIN
        -- Ảnh thi thể nhạy cảm (LaNhayCam = 1)
        INSERT INTO dbo.HinhAnh 
        (TenFile, TenGoc, DuongDan, LoaiHinhAnh, DinhDang, KichThuoc, ChieuRong, ChieuCao, HashMD5, LaNhayCam, MoTa, MaHoSoThiThe, MaTaiKhoanTaiLen, TrangThai)
        VALUES
         ('guid_body_exam_01.jpg', 'anh_kham_nghiem_vet_seo.jpg', '/secure-storage/bodies/guid_body_exam_01.jpg', N'ThiThe_KhamNghiem', 'image/jpeg', 1048576, 1920, 1080, '4a7d1ed414474e4033ac29ccb8653d9b', 1, N'Ảnh chụp cận cảnh vết sẹo cẳng tay trái', @Body1, @AccCanBo, N'HoatDong'),
         ('guid_body_face_01.jpg', 'anh_khuon_mat_thi_the.jpg',  '/secure-storage/bodies/guid_body_face_01.jpg', N'ThiThe_KhuonMat',   'image/jpeg', 854200, 1280, 720,  'b10a8db164e0754105b7a99be72e3fe5', 1, N'Ảnh khuôn mặt tử thi phục vụ đối chiếu Face AI', @Body1, @AccCanBo, N'HoatDong');
    END

    IF @AccDan IS NOT NULL AND @Missing1 IS NOT NULL
    BEGIN
        -- Ảnh người mất tích công khai (LaNhayCam = 0)
        INSERT INTO dbo.HinhAnh 
        (TenFile, TenGoc, DuongDan, LoaiHinhAnh, DinhDang, KichThuoc, ChieuRong, ChieuCao, HashMD5, LaNhayCam, MoTa, MaHoSoMatTich, MaTaiKhoanTaiLen, TrangThai)
        VALUES
         ('guid_missing_portrait_01.jpg', 'anh_the_chan_dung.jpg', '/public-storage/missing/guid_missing_portrait_01.jpg', N'MatTich_ChanDung', 'image/jpeg', 524288, 800, 1000, '9e107d9d372bb6826bd81d3542a419d6', 0, N'Ảnh chân dung thẻ chụp năm 2025', @Missing1, @AccDan, N'HoatDong');
    END
END

/* 10. THÔNG BÁO VÀ NHẬT KÝ MẪU */
IF NOT EXISTS (SELECT 1 FROM dbo.ThongBao)
BEGIN
    DECLARE @AccCanBoTb BIGINT = (SELECT TOP 1 MaTaiKhoan FROM dbo.TaiKhoanNguoiDung WHERE TenDangNhap = 'canbo01');
    DECLARE @AccDanTb BIGINT   = (SELECT TOP 1 MaTaiKhoan FROM dbo.TaiKhoanNguoiDung WHERE TenDangNhap = 'dan01');

    IF @AccCanBoTb IS NOT NULL
        INSERT INTO dbo.ThongBao (NoiDung, MaTaiKhoan) 
        VALUES (N'Hệ thống AI đề xuất: Hồ sơ mất tích MT-001 có độ tương đồng 78.5% với tử thi TT-001. Vui lòng rà soát xác minh.', @AccCanBoTb);

    IF @AccDanTb IS NOT NULL
        INSERT INTO dbo.ThongBao (NoiDung, MaTaiKhoan) 
        VALUES (N'Yêu cầu tìm người thân của bạn đã được cán bộ chuyên trách tiếp nhận và đang tiến hành tra cứu trên hệ thống.', @AccDanTb);
END

IF NOT EXISTS (SELECT 1 FROM dbo.NhatKyHeThong)
BEGIN
    DECLARE @AdminLogId BIGINT = (SELECT TOP 1 MaTaiKhoan FROM dbo.TaiKhoanNguoiDung WHERE TenDangNhap = 'admin');
    IF @AdminLogId IS NOT NULL
    BEGIN
        INSERT INTO dbo.NhatKyHeThong (HanhDong, DoiTuongTacDong, MaTaiKhoan)
        VALUES 
         (N'Khởi tạo dữ liệu hệ thống ban đầu', N'SystemSeed', @AdminLogId),
         (N'Đăng nhập trang quản trị', CONCAT(N'TaiKhoan#', @AdminLogId), @AdminLogId);
    END
END

-- Lịch sử đăng nhập mẫu
IF OBJECT_ID(N'dbo.LichSuDangNhap', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.LichSuDangNhap)
BEGIN
    DECLARE @AdminLg BIGINT = (SELECT TOP 1 MaTaiKhoan FROM dbo.TaiKhoanNguoiDung WHERE TenDangNhap = 'admin');
    IF @AdminLg IS NOT NULL
    BEGIN
        INSERT INTO dbo.LichSuDangNhap (TenDangNhap, MaTaiKhoan, DiaChiIP, UserAgent, ThanhCong, LyDoThatBai)
        VALUES 
         ('admin', @AdminLg, '127.0.0.1', 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) Edge/120.0', 1, NULL),
         ('canbo01', NULL,    '127.0.0.1', 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/120.0', 0, N'Sai mật khẩu');
    END
END
GO