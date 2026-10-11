USE QL_XACMINHTHITHE;
GO
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
 
-- Ghi nhat ky thu cong (vi du: dang nhap, xem ho so nhay cam)
CREATE OR ALTER PROCEDURE sp_GhiNhatKy
    @HanhDong NVARCHAR(100), @DoiTuongTacDong NVARCHAR(255) = NULL, @MaTaiKhoan BIGINT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT NhatKyHeThong (HanhDong, DoiTuongTacDong, MaTaiKhoan)
    VALUES (@HanhDong, @DoiTuongTacDong, ISNULL(@MaTaiKhoan, fn_NguoiThaoTac()));
END
GO

CREATE OR ALTER PROCEDURE sp_TaoThongBao
    @MaTaiKhoan BIGINT, @NoiDung NVARCHAR(1000)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT ThongBao (NoiDung, MaTaiKhoan) VALUES (@NoiDung, @MaTaiKhoan);
END
GO

-- Cong an xu ly tin bao: DaTiepNhan / TuChoi / YeuCauBoSung / DangXacMinh / DaXuLy
CREATE OR ALTER PROCEDURE sp_XuLyTinBao
    @MaTin BIGINT, @MaTaiKhoan BIGINT, @QuyetDinh NVARCHAR(30)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF fn_CoQuyen(@MaTaiKhoan, 'INCIDENT', 'CONFIRM') = 0
        THROW 50403, N'Tai khoan khong co quyen xu ly tin bao', 1;
    IF @QuyetDinh NOT IN (N'DaTiepNhan', N'TuChoi', N'YeuCauBoSung', N'DangXacMinh', N'DaXuLy')
        THROW 50400, N'Quyet dinh khong hop le', 1;
    IF NOT EXISTS (SELECT 1 FROM TinBao WHERE MaTin = @MaTin)
        THROW 50404, N'Khong tim thay tin bao', 1;

    EXEC sys.sp_set_session_context @key = N'user_id', @value = @MaTaiKhoan;

    UPDATE TinBao
       SET TrangThai = @QuyetDinh,
           MaTaiKhoanTiepNhan = ISNULL(MaTaiKhoanTiepNhan, @MaTaiKhoan)
     WHERE MaTin = @MaTin;
END
GO

-- Lap ho so thi the (co the gan voi tin bao)
CREATE OR ALTER PROCEDURE sp_LapHoSoThiThe
    @MaTaiKhoan BIGINT, @ThoiGianPhatHien DATETIME2, @DiaDiem NVARCHAR(255),
    @GioiTinh NVARCHAR(10), @TuoiUocLuong SMALLINT = NULL, @ChieuCao DECIMAL(5,2) = NULL,
    @DacDiemCoThe NVARCHAR(MAX) = NULL, @MaTin BIGINT = NULL,
    @MaHoSoThiThe BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF fn_CoQuyen(@MaTaiKhoan, 'BODY', 'CREATE') = 0
        THROW 50403, N'Tai khoan khong co quyen lap ho so thi the', 1;

    EXEC sys.sp_set_session_context @key = N'user_id', @value = @MaTaiKhoan;

    INSERT HoSoThiThe (ThoiGianPhatHien, DiaDiem, GioiTinh, TuoiUocLuong, ChieuCao, DacDiemCoThe, MaTin)
    VALUES (@ThoiGianPhatHien, @DiaDiem, @GioiTinh, @TuoiUocLuong, @ChieuCao, @DacDiemCoThe, @MaTin);

    SET @MaHoSoThiThe = SCOPE_IDENTITY();
END
GO

-- Xep / chuyen vi tri luu tru (vi tri cu tu giai phong vi tri hien tai la dong lich su moi nhat)
CREATE OR ALTER PROCEDURE sp_XepViTri
    @MaHoSoThiThe BIGINT, @MaViTri INT, @MaTaiKhoan BIGINT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF fn_CoQuyen(@MaTaiKhoan, 'STORAGE', 'UPDATE') = 0
        THROW 50403, N'Tai khoan khong co quyen xep vi tri luu tru', 1;
    IF NOT EXISTS (SELECT 1 FROM HoSoThiThe WHERE MaHoSoThiThe = @MaHoSoThiThe)
        THROW 50404, N'Khong tim thay ho so thi the', 1;
    IF NOT EXISTS (SELECT 1 FROM ViTriLuuTru WHERE MaViTri = @MaViTri)
        THROW 50404, N'Khong tim thay vi tri luu tru', 1;
    IF EXISTS (SELECT 1 FROM HoSoThiThe WHERE MaHoSoThiThe = @MaHoSoThiThe AND TrangThai IN (N'DaBanGiao', N'DongHoSo'))
        THROW 50400, N'Ho so da ban giao hoac da dong, khong xep vi tri duoc', 1;

    EXEC sys.sp_set_session_context @key = N'user_id', @value = @MaTaiKhoan;

    INSERT LichSuLuuTru (MaHoSoThiThe, MaViTri) VALUES (@MaHoSoThiThe, @MaViTri);
END
GO

-- Chay doi sanh 1 thi the voi cac ho so nguoi mat tich dang tim kiem.
-- Bo qua cap da bi loai (LoaiBo); cap dang cho xu ly thi cap nhat lai diem; cap da chuyen xac minh giu nguyen.
CREATE OR ALTER PROCEDURE sp_ChayDoiSanh
    @MaHoSoThiThe BIGINT, @MaTaiKhoan BIGINT, @DiemToiThieu DECIMAL(5,2) = 0
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF fn_CoQuyen(@MaTaiKhoan, 'MATCHING', 'CREATE') = 0
        THROW 50403, N'Tai khoan khong co quyen chay doi sanh', 1;
    IF NOT EXISTS (SELECT 1 FROM HoSoThiThe WHERE MaHoSoThiThe = @MaHoSoThiThe)
        THROW 50404, N'Khong tim thay ho so thi the', 1;

    EXEC sys.sp_set_session_context @key = N'user_id', @value = @MaTaiKhoan;

    IF OBJECT_ID('tempdb..#KetQuaDoiSanh') IS NOT NULL DROP TABLE #KetQuaDoiSanh;

    SELECT s.MaHoSoMatTich, s.DiemCoBan, s.DiemDiaDiem, s.DiemThoiGian,
           fn_TongDiem(s.DiemCoBan, s.DiemDiaDiem, s.DiemThoiGian) AS TongDiem
    INTO #KetQuaDoiSanh
    FROM (SELECT m.MaHoSoMatTich,
                 fn_DiemCoBan(@MaHoSoThiThe, m.MaHoSoMatTich)   AS DiemCoBan,
                 fn_DiemDiaDiem(@MaHoSoThiThe, m.MaHoSoMatTich) AS DiemDiaDiem,
                 fn_DiemThoiGian(@MaHoSoThiThe, m.MaHoSoMatTich) AS DiemThoiGian
          FROM HoSoNguoiMatTich m
          WHERE m.TrangThai IN (N'DangTimKiem', N'CoHoSoNghiVan')
            AND NOT EXISTS (SELECT 1 FROM DoiSanh x
                            WHERE x.MaHoSoThiThe = @MaHoSoThiThe
                              AND x.MaHoSoMatTich = m.MaHoSoMatTich
                              AND x.TrangThai = N'LoaiBo')) s;

    DELETE FROM #KetQuaDoiSanh WHERE TongDiem IS NULL OR TongDiem < @DiemToiThieu;

    BEGIN TRAN;

    UPDATE ds
       SET ds.DiemDoiSanh = k.TongDiem,
           ds.TieuChi = CONCAT(N'CoBan=', ISNULL(CONVERT(VARCHAR(10), k.DiemCoBan), '-'),
                               N'; DiaDiem=', ISNULL(CONVERT(VARCHAR(10), k.DiemDiaDiem), '-'),
                               N'; ThoiGian=', ISNULL(CONVERT(VARCHAR(10), k.DiemThoiGian), '-')),
           ds.ThoiGianYeuCau = SYSDATETIME(),
           ds.MaTaiKhoanYeuCau = @MaTaiKhoan
    FROM DoiSanh ds
    JOIN #KetQuaDoiSanh k ON k.MaHoSoMatTich = ds.MaHoSoMatTich
    WHERE ds.MaHoSoThiThe = @MaHoSoThiThe AND ds.TrangThai = N'ChoXuLy';

    INSERT DoiSanh (TieuChi, DiemDoiSanh, MaHoSoThiThe, MaHoSoMatTich, MaTaiKhoanYeuCau)
    SELECT CONCAT(N'CoBan=', ISNULL(CONVERT(VARCHAR(10), k.DiemCoBan), '-'),
                  N'; DiaDiem=', ISNULL(CONVERT(VARCHAR(10), k.DiemDiaDiem), '-'),
                  N'; ThoiGian=', ISNULL(CONVERT(VARCHAR(10), k.DiemThoiGian), '-')),
           k.TongDiem, @MaHoSoThiThe, k.MaHoSoMatTich, @MaTaiKhoan
    FROM #KetQuaDoiSanh k
    WHERE NOT EXISTS (SELECT 1 FROM DoiSanh ds
                      WHERE ds.MaHoSoThiThe = @MaHoSoThiThe AND ds.MaHoSoMatTich = k.MaHoSoMatTich);

    COMMIT;

    SELECT ds.MaDoiSanh, ds.MaHoSoMatTich, mt.HoTen, ds.DiemDoiSanh, ds.MucDoPhuHop, ds.TrangThai, ds.TieuChi
    FROM DoiSanh ds
    JOIN HoSoNguoiMatTich mt ON mt.MaHoSoMatTich = ds.MaHoSoMatTich
    WHERE ds.MaHoSoThiThe = @MaHoSoThiThe
    ORDER BY ds.DiemDoiSanh DESC;
END
GO

-- Chuyen cap doi sanh sang phien xac minh
CREATE OR ALTER PROCEDURE sp_TaoXacMinh
    @MaDoiSanh BIGINT, @MaTaiKhoan BIGINT, @PhuongPhap NVARCHAR(100) = NULL,
    @MaXacMinh BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF fn_CoQuyen(@MaTaiKhoan, 'VERIFICATION', 'CREATE') = 0
        THROW 50403, N'Tai khoan khong co quyen tao phien xac minh', 1;
    IF NOT EXISTS (SELECT 1 FROM DoiSanh WHERE MaDoiSanh = @MaDoiSanh)
        THROW 50404, N'Khong tim thay ket qua doi sanh', 1;

    EXEC sys.sp_set_session_context @key = N'user_id', @value = @MaTaiKhoan;

    INSERT XacMinh (PhuongPhap, MaDoiSanh, MaTaiKhoanThucHien)
    VALUES (@PhuongPhap, @MaDoiSanh, @MaTaiKhoan);

    SET @MaXacMinh = SCOPE_IDENTITY();
END
GO

-- Ket luan phien xac minh: Khop -> tu dong trinh duyet (ChoDuyet); KhongKhop -> loai cap, khong de xuat lai
CREATE OR ALTER PROCEDURE sp_KetLuanXacMinh
    @MaXacMinh BIGINT, @MaTaiKhoan BIGINT, @KetLuan NVARCHAR(10),
    @NhanXet NVARCHAR(1000) = NULL, @MucDoChacChan DECIMAL(5,2) = NULL, @BangChung NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF fn_CoQuyen(@MaTaiKhoan, 'VERIFICATION', 'CONFIRM') = 0
        THROW 50403, N'Tai khoan khong co quyen ket luan xac minh', 1;

    DECLARE @tt NVARCHAR(30), @nguoiTH BIGINT;
    SELECT @tt = TrangThai, @nguoiTH = MaTaiKhoanThucHien FROM XacMinh WHERE MaXacMinh = @MaXacMinh;

    IF @tt IS NULL THROW 50404, N'Khong tim thay phien xac minh', 1;
    IF @nguoiTH <> @MaTaiKhoan THROW 50403, N'Chi nguoi thuc hien xac minh moi duoc ket luan', 1;
    IF @tt NOT IN (N'DangXacMinh', N'TuChoiDuyet')
        THROW 50400, N'Phien xac minh o trang thai hien tai khong the ket luan', 1;

    EXEC sys.sp_set_session_context @key = N'user_id', @value = @MaTaiKhoan;

    IF @KetLuan = N'Khop'
    BEGIN
        UPDATE XacMinh
           SET KetQua = N'Khop', NhanXet = @NhanXet, MucDoChacChan = @MucDoChacChan,
               BangChung = @BangChung, TrangThai = N'ChoDuyet'
         WHERE MaXacMinh = @MaXacMinh;
    END
    ELSE IF @KetLuan = N'KhongKhop'
    BEGIN
        IF NULLIF(LTRIM(RTRIM(@NhanXet)), N'') IS NULL
            THROW 50400, N'Bat buoc nhap ly do khi ket luan khong khop', 1;
        UPDATE XacMinh
           SET KetQua = N'KhongKhop', NhanXet = @NhanXet, MucDoChacChan = @MucDoChacChan,
               BangChung = @BangChung, TrangThai = N'KhongPhuHop'
         WHERE MaXacMinh = @MaXacMinh;
    END
    ELSE
        THROW 50400, N'Ket luan khong hop le (Khop / KhongKhop)', 1;
END
GO

-- Cap co tham quyen (quyen VERIFICATION/APPROVE) duyet hoac tu choi ket luan danh tinh.
-- Moi xac minh chi co 1 dong phe duyet (quan he 1-1): tu choi roi duyet lai thi cap nhat dong cu.
CREATE OR ALTER PROCEDURE sp_DuyetKetLuan
    @MaXacMinh BIGINT, @MaNguoiDuyet BIGINT, @QuyetDinh NVARCHAR(10), @YKien NVARCHAR(1000) = NULL
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF fn_CoQuyen(@MaNguoiDuyet, 'VERIFICATION', 'APPROVE') = 0
        THROW 50403, N'Tai khoan khong co quyen duyet ket luan', 1;
    IF @QuyetDinh NOT IN (N'Duyet', N'TuChoi')
        THROW 50400, N'Quyet dinh khong hop le (Duyet / TuChoi)', 1;
    IF NOT EXISTS (SELECT 1 FROM XacMinh WHERE MaXacMinh = @MaXacMinh)
        THROW 50404, N'Khong tim thay phien xac minh', 1;
    IF NOT EXISTS (SELECT 1 FROM XacMinh WHERE MaXacMinh = @MaXacMinh AND TrangThai = N'ChoDuyet')
        THROW 50400, N'Phien xac minh khong o trang thai cho duyet', 1;

    EXEC sys.sp_set_session_context @key = N'user_id', @value = @MaNguoiDuyet;

    IF EXISTS (SELECT 1 FROM PheDuyet WHERE MaXacMinh = @MaXacMinh)
        UPDATE PheDuyet
           SET ThoiGian = SYSDATETIME(), TrangThai = @QuyetDinh, YKien = @YKien, MaNguoiDuyet = @MaNguoiDuyet
         WHERE MaXacMinh = @MaXacMinh;
    ELSE
        INSERT PheDuyet (TrangThai, YKien, MaXacMinh, MaNguoiDuyet)
        VALUES (@QuyetDinh, @YKien, @MaXacMinh, @MaNguoiDuyet);
END
GO

-- Lap phieu ban giao (chi khi ket luan da duoc duyet; nguoi nhan phai la nguoi than cua ho so mat tich)
CREATE OR ALTER PROCEDURE sp_LapBanGiao
    @MaXacMinh BIGINT, @MaNguoiThan BIGINT, @MaTaiKhoan BIGINT, @MaNguoiGiao BIGINT,
    @DiaDiem NVARCHAR(255) = NULL, @MaBanGiao BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF fn_CoQuyen(@MaTaiKhoan, 'HANDOVER', 'CREATE') = 0
        THROW 50403, N'Tai khoan khong co quyen lap phieu ban giao', 1;

    EXEC sys.sp_set_session_context @key = N'user_id', @value = @MaTaiKhoan;

    INSERT BanGiao (DiaDiem, MaXacMinh, MaNguoiThan, MaNguoiGiao)
    VALUES (@DiaDiem, @MaXacMinh, @MaNguoiThan, @MaNguoiGiao);

    SET @MaBanGiao = SCOPE_IDENTITY();
END
GO

-- Cac buoc ban giao: KIEMTRA (nha xac kiem tra ho so) -> GIAO (nha xac giao) -> XACNHAN (xac nhan hoan tat) | HUY
CREATE OR ALTER PROCEDURE sp_BuocBanGiao
    @MaBanGiao BIGINT, @MaTaiKhoan BIGINT, @Buoc VARCHAR(10), @DiaDiem NVARCHAR(255) = NULL
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS (SELECT 1 FROM BanGiao WHERE MaBanGiao = @MaBanGiao)
        THROW 50404, N'Khong tim thay phieu ban giao', 1;

    EXEC sys.sp_set_session_context @key = N'user_id', @value = @MaTaiKhoan;

    IF @Buoc = 'KIEMTRA'
    BEGIN
        IF fn_CoQuyen(@MaTaiKhoan, 'HANDOVER', 'UPDATE') = 0 THROW 50403, N'Khong co quyen kiem tra ban giao', 1;
        UPDATE BanGiao SET TrangThai = N'DangKiemTra' WHERE MaBanGiao = @MaBanGiao;
    END
    ELSE IF @Buoc = 'GIAO'
    BEGIN
        IF fn_CoQuyen(@MaTaiKhoan, 'HANDOVER', 'HANDOVER') = 0 THROW 50403, N'Khong co quyen giao thi the', 1;
        UPDATE BanGiao
           SET TrangThai = N'DaGiao', ThoiGian = SYSDATETIME(), DiaDiem = ISNULL(@DiaDiem, DiaDiem)
         WHERE MaBanGiao = @MaBanGiao;
    END
    ELSE IF @Buoc = 'XACNHAN'
    BEGIN
        IF fn_CoQuyen(@MaTaiKhoan, 'HANDOVER', 'CONFIRM') = 0 THROW 50403, N'Khong co quyen xac nhan ban giao', 1;
        UPDATE BanGiao SET TrangThai = N'DaXacNhan' WHERE MaBanGiao = @MaBanGiao;
    END
    ELSE IF @Buoc = 'HUY'
    BEGIN
        IF fn_CoQuyen(@MaTaiKhoan, 'HANDOVER', 'CREATE') = 0 THROW 50403, N'Khong co quyen huy ban giao', 1;
        UPDATE BanGiao SET TrangThai = N'Huy' WHERE MaBanGiao = @MaBanGiao;
    END
    ELSE
        THROW 50400, N'Buoc ban giao khong hop le (KIEMTRA / GIAO / XACNHAN / HUY)', 1;
END
GO

-- Dong ho so thi the sau khi ban giao (khoa ho so, trigger dong ho so nguoi mat tich)
CREATE OR ALTER PROCEDURE sp_DongHoSoThiThe
    @MaHoSoThiThe BIGINT, @MaTaiKhoan BIGINT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF fn_CoQuyen(@MaTaiKhoan, 'BODY', 'UPDATE') = 0
        THROW 50403, N'Tai khoan khong co quyen dong ho so', 1;
    IF NOT EXISTS (SELECT 1 FROM HoSoThiThe WHERE MaHoSoThiThe = @MaHoSoThiThe AND TrangThai = N'DaBanGiao')
        THROW 50400, N'Chi dong duoc ho so o trang thai DaBanGiao', 1;

    EXEC sys.sp_set_session_context @key = N'user_id', @value = @MaTaiKhoan;

    UPDATE HoSoThiThe SET TrangThai = N'DongHoSo' WHERE MaHoSoThiThe = @MaHoSoThiThe;
END
GO

-- Mo lai ho so da dong (bat buoc co ly do, ly do duoc ghi vao nhat ky)
CREATE OR ALTER PROCEDURE sp_MoLaiHoSoThiThe
    @MaHoSoThiThe BIGINT, @MaTaiKhoan BIGINT, @LyDo NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF fn_CoQuyen(@MaTaiKhoan, 'BODY', 'UPDATE') = 0
        THROW 50403, N'Tai khoan khong co quyen mo lai ho so', 1;
    IF NULLIF(LTRIM(RTRIM(@LyDo)), N'') IS NULL
        THROW 50400, N'Mo lai ho so bat buoc nhap ly do', 1;
    IF NOT EXISTS (SELECT 1 FROM HoSoThiThe WHERE MaHoSoThiThe = @MaHoSoThiThe AND TrangThai = N'DongHoSo')
        THROW 50400, N'Ho so khong o trang thai da dong', 1;

    EXEC sys.sp_set_session_context @key = N'user_id', @value = @MaTaiKhoan;

    BEGIN TRAN;
    UPDATE HoSoThiThe SET TrangThai = N'TiepTucLuuGiu' WHERE MaHoSoThiThe = @MaHoSoThiThe;
    INSERT NhatKyHeThong (HanhDong, DoiTuongTacDong, MaTaiKhoan)
    VALUES (N'MO_LAI_HO_SO', LEFT(CONCAT(N'HoSoThiThe#', @MaHoSoThiThe, N': ', @LyDo), 255), @MaTaiKhoan);
    COMMIT;
END
GO

/* =========================================================================
   THỦ TỤC NGHIỆP VỤ: ACCOUNT / PERMISSION (Phần 3.3)
   ========================================================================= */

-- Dang nhap & chong brute-force
CREATE OR ALTER PROCEDURE sp_DangNhap
    @TenDangNhap VARCHAR(50),
    @DiaChiIP VARCHAR(45) = NULL,
    @UserAgent NVARCHAR(500) = NULL,
    @MatKhauHopLe BIT,
    @KetQua INT OUTPUT,
    @ThongBao NVARCHAR(255) OUTPUT,
    @MaTaiKhoan BIGINT OUTPUT,
    @MaVaiTro INT OUTPUT,
    @HoTen NVARCHAR(150) OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    DECLARE @TrangThai NVARCHAR(20);
    DECLARE @SoLanSai INT;
    DECLARE @KhoaDen DATETIME2;
    DECLARE @HienTai DATETIME2 = SYSDATETIME();

    SELECT @MaTaiKhoan = MaTaiKhoan,
           @MaVaiTro = MaVaiTro,
           @HoTen = HoTen,
           @TrangThai = TrangThai,
           @SoLanSai = ISNULL(SoLanDangNhapSai, 0),
           @KhoaDen = KhoaDenThoiDiem
    FROM TaiKhoanNguoiDung
    WHERE TenDangNhap = @TenDangNhap OR Email = @TenDangNhap;

    IF @MaTaiKhoan IS NULL
    BEGIN
        SET @KetQua = -1;
        SET @ThongBao = N'Tai khoan hoac mat khau khong chinh xac';
        INSERT INTO LichSuDangNhap (TenDangNhap, MaTaiKhoan, DiaChiIP, UserAgent, ThanhCong, LyDoThatBai)
        VALUES (@TenDangNhap, NULL, @DiaChiIP, @UserAgent, 0, N'Tai khoan khong ton tai');
        RETURN;
    END

    IF @TrangThai = N'Khoa'
    BEGIN
        SET @KetQua = -2;
        SET @ThongBao = N'Tai khoan da bi khoa boi Quan tri vien.';
        INSERT INTO LichSuDangNhap (TenDangNhap, MaTaiKhoan, DiaChiIP, UserAgent, ThanhCong, LyDoThatBai)
        VALUES (@TenDangNhap, @MaTaiKhoan, @DiaChiIP, @UserAgent, 0, N'Tai khoan bi khoa');
        RETURN;
    END

    IF @KhoaDen IS NOT NULL AND @KhoaDen > @HienTai
    BEGIN
        SET @KetQua = -3;
        SET @ThongBao = CONCAT(N'Tai khoan dang bi tam khoa den ', FORMAT(@KhoaDen, 'HH:mm:ss dd/MM/yyyy'), N' do nhap sai qua 5 lan.');
        INSERT INTO LichSuDangNhap (TenDangNhap, MaTaiKhoan, DiaChiIP, UserAgent, ThanhCong, LyDoThatBai)
        VALUES (@TenDangNhap, @MaTaiKhoan, @DiaChiIP, @UserAgent, 0, N'Dang trong thoi gian khoa tam');
        RETURN;
    END

    IF @KhoaDen IS NOT NULL AND @KhoaDen <= @HienTai AND @TrangThai = N'KhoaTam'
    BEGIN
        UPDATE TaiKhoanNguoiDung
        SET TrangThai = N'HoatDong', SoLanDangNhapSai = 0, KhoaDenThoiDiem = NULL
        WHERE MaTaiKhoan = @MaTaiKhoan;
        SET @SoLanSai = 0;
    END

    IF @MatKhauHopLe = 1
    BEGIN
        UPDATE TaiKhoanNguoiDung
        SET SoLanDangNhapSai = 0, KhoaDenThoiDiem = NULL, LanDangNhapCuoi = @HienTai
        WHERE MaTaiKhoan = @MaTaiKhoan;

        SET @KetQua = 1;
        SET @ThongBao = N'Dang nhap thanh cong';

        INSERT INTO LichSuDangNhap (TenDangNhap, MaTaiKhoan, DiaChiIP, UserAgent, ThanhCong, LyDoThatBai)
        VALUES (@TenDangNhap, @MaTaiKhoan, @DiaChiIP, @UserAgent, 1, NULL);

        INSERT INTO NhatKyHeThong (HanhDong, DoiTuongTacDong, MaTaiKhoan)
        VALUES (N'DANG_NHAP_THANH_CONG', CONCAT(N'TaiKhoan#', @MaTaiKhoan), @MaTaiKhoan);
    END
    ELSE
    BEGIN
        SET @SoLanSai = @SoLanSai + 1;
        IF @SoLanSai >= 5
        BEGIN
            SET @KhoaDen = DATEADD(MINUTE, 15, @HienTai);
            UPDATE TaiKhoanNguoiDung
            SET SoLanDangNhapSai = @SoLanSai, TrangThai = N'KhoaTam', KhoaDenThoiDiem = @KhoaDen
            WHERE MaTaiKhoan = @MaTaiKhoan;

            SET @KetQua = -3;
            SET @ThongBao = N'Nhap sai mat khau 5 lan lien tiep. Tai khoan bi tam khoa 15 phut.';
        END
        ELSE
        BEGIN
            UPDATE TaiKhoanNguoiDung SET SoLanDangNhapSai = @SoLanSai WHERE MaTaiKhoan = @MaTaiKhoan;
            SET @KetQua = 0;
            SET @ThongBao = CONCAT(N'Mat khau khong dung. Con ', 5 - @SoLanSai, N' lan thu.');
        END

        INSERT INTO LichSuDangNhap (TenDangNhap, MaTaiKhoan, DiaChiIP, UserAgent, ThanhCong, LyDoThatBai)
        VALUES (@TenDangNhap, @MaTaiKhoan, @DiaChiIP, @UserAgent, 0, N'Sai mat khau');
    END
END
GO

-- Dang ky tai khoan nguoi dan
CREATE OR ALTER PROCEDURE sp_DangKyTaiKhoan
    @TenDangNhap VARCHAR(50),
    @MatKhauHash VARCHAR(255),
    @HoTen NVARCHAR(150),
    @Email VARCHAR(150) = NULL,
    @SoDienThoai VARCHAR(20) = NULL,
    @CCCD VARCHAR(20) = NULL,
    @MaTaiKhoanMoi BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF EXISTS (SELECT 1 FROM TaiKhoanNguoiDung WHERE TenDangNhap = @TenDangNhap)
        THROW 50409, N'Ten dang nhap nay da ton tai.', 1;
    IF @Email IS NOT NULL AND EXISTS (SELECT 1 FROM TaiKhoanNguoiDung WHERE Email = @Email)
        THROW 50409, N'Email nay da duoc su dung.', 1;
    IF @SoDienThoai IS NOT NULL AND EXISTS (SELECT 1 FROM TaiKhoanNguoiDung WHERE SoDienThoai = @SoDienThoai)
        THROW 50409, N'So dien thoai nay da duoc su dung.', 1;
    IF @CCCD IS NOT NULL AND EXISTS (SELECT 1 FROM TaiKhoanNguoiDung WHERE CCCD = @CCCD)
        THROW 50409, N'So CCCD nay da ton tai.', 1;

    DECLARE @MaVaiTro INT;
    SELECT @MaVaiTro = MaVaiTro FROM VaiTro WHERE TenVaiTro LIKE N'%Người dân%' OR TenVaiTro = N'NGUOI_DAN';
    IF @MaVaiTro IS NULL SELECT TOP 1 @MaVaiTro = MaVaiTro FROM VaiTro WHERE MaVaiTro = 5;

    INSERT INTO TaiKhoanNguoiDung (TenDangNhap, MatKhauHash, HoTen, Email, SoDienThoai, CCCD, MaVaiTro, TrangThai, NgayTao)
    VALUES (@TenDangNhap, @MatKhauHash, @HoTen, @Email, @SoDienThoai, @CCCD, @MaVaiTro, N'HoatDong', SYSDATETIME());

    SET @MaTaiKhoanMoi = SCOPE_IDENTITY();
    INSERT INTO NhatKyHeThong (HanhDong, DoiTuongTacDong, MaTaiKhoan)
    VALUES (N'DANG_KY_TAI_KHOAN', CONCAT(N'TaiKhoan#', @MaTaiKhoanMoi), @MaTaiKhoanMoi);
END
GO

-- Doi mat khau
CREATE OR ALTER PROCEDURE sp_DoiMatKhau
    @MaTaiKhoan BIGINT,
    @MatKhauMoiHash VARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS (SELECT 1 FROM TaiKhoanNguoiDung WHERE MaTaiKhoan = @MaTaiKhoan)
        THROW 50404, N'Khong tim thay tai khoan can doi mat khau.', 1;

    UPDATE TaiKhoanNguoiDung
    SET MatKhauHash = @MatKhauMoiHash, NgayCapNhat = SYSDATETIME()
    WHERE MaTaiKhoan = @MaTaiKhoan;

    INSERT INTO NhatKyHeThong (HanhDong, DoiTuongTacDong, MaTaiKhoan)
    VALUES (N'DOI_MAT_KHAU', CONCAT(N'TaiKhoan#', @MaTaiKhoan), @MaTaiKhoan);
END
GO

-- Khoa / Mo khoa tai khoan
CREATE OR ALTER PROCEDURE sp_KhoaMoKhoaTaiKhoan
    @MaTaiKhoanAdmin BIGINT,
    @MaTaiKhoanMucTieu BIGINT,
    @TrangThaiMoi NVARCHAR(20),
    @LyDo NVARCHAR(255) = NULL
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF fn_CoQuyen(@MaTaiKhoanAdmin, 'ACCOUNT', 'UPDATE') = 0
        THROW 50403, N'Tai khoan khong co quyen quan ly trang thai tai khoan.', 1;
    IF @TrangThaiMoi NOT IN (N'HoatDong', N'Khoa')
        THROW 50400, N'Trang thai cap nhat khong hop le.', 1;
    IF @MaTaiKhoanAdmin = @MaTaiKhoanMucTieu AND @TrangThaiMoi = N'Khoa'
        THROW 50400, N'Quan tri vien khong the tu khoa tai khoan chinh minh.', 1;

    UPDATE TaiKhoanNguoiDung
    SET TrangThai = @TrangThaiMoi, SoLanDangNhapSai = 0, KhoaDenThoiDiem = NULL, NgayCapNhat = SYSDATETIME()
    WHERE MaTaiKhoan = @MaTaiKhoanMucTieu;

    INSERT INTO NhatKyHeThong (HanhDong, DoiTuongTacDong, MaTaiKhoan)
    VALUES (CONCAT(N'DOI_TRANG_THAI_', @TrangThaiMoi, N': ', ISNULL(@LyDo, N'')), CONCAT(N'TaiKhoan#', @MaTaiKhoanMucTieu), @MaTaiKhoanAdmin);
END
GO

-- Phan quyen vai tro
CREATE OR ALTER PROCEDURE sp_PhanQuyenVaiTro
    @MaTaiKhoanAdmin BIGINT,
    @MaVaiTro INT,
    @MaQuyen INT,
    @PhamVi VARCHAR(10) = 'ALL',
    @HanhDong VARCHAR(10) -- 'GAN' hoac 'HUY'
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF fn_CoQuyen(@MaTaiKhoanAdmin, 'PERMISSION', 'UPDATE') = 0
        THROW 50403, N'Tai khoan khong co quyen phan quyen.', 1;

    IF @HanhDong = 'GAN'
    BEGIN
        IF EXISTS (SELECT 1 FROM VaiTro_Quyen WHERE MaVaiTro = @MaVaiTro AND MaQuyen = @MaQuyen)
            UPDATE VaiTro_Quyen SET PhamVi = @PhamVi WHERE MaVaiTro = @MaVaiTro AND MaQuyen = @MaQuyen;
        ELSE
            INSERT INTO VaiTro_Quyen (MaVaiTro, MaQuyen, PhamVi) VALUES (@MaVaiTro, @MaQuyen, @PhamVi);
    END
    ELSE IF @HanhDong = 'HUY'
    BEGIN
        DELETE FROM VaiTro_Quyen WHERE MaVaiTro = @MaVaiTro AND MaQuyen = @MaQuyen;
    END

    INSERT INTO NhatKyHeThong (HanhDong, DoiTuongTacDong, MaTaiKhoan)
    VALUES (CONCAT(@HanhDong, N'_QUYEN#', @MaQuyen, N'_VAITRO#', @MaVaiTro), N'VaiTro_Quyen', @MaTaiKhoanAdmin);
END
GO

/* =========================================================================
   THỦ TỤC NGHIỆP VỤ: HÌNH ẢNH & TỆP ĐÍNH KÈM (Phần 3.7)
   ========================================================================= */

-- Them moi hinh anh
CREATE OR ALTER PROCEDURE sp_ThemHinhAnh
    @TenFile NVARCHAR(255),
    @TenGoc NVARCHAR(255),
    @DuongDan NVARCHAR(500),
    @LoaiHinhAnh NVARCHAR(50),
    @DinhDang VARCHAR(30),
    @KichThuoc BIGINT,
    @ChieuRong INT = NULL,
    @ChieuCao INT = NULL,
    @HashMD5 VARCHAR(64) = NULL,
    @LaNhayCam BIT = 0,
    @MoTa NVARCHAR(500) = NULL,
    @MaHoSoThiThe BIGINT = NULL,
    @MaHoSoMatTich BIGINT = NULL,
    @MaTinBao INT = NULL,
    @MaXacMinh BIGINT = NULL,
    @MaTaiKhoanTaiLen BIGINT,
    @MaHinhAnhMoi BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF fn_CoQuyen(@MaTaiKhoanTaiLen, 'FILE', 'CREATE') = 0
        THROW 50403, N'Tai khoan khong co quyen tai hinh anh len.', 1;

    IF @MaHoSoThiThe IS NOT NULL
        SET @LaNhayCam = 1;

    DECLARE @MaxMB DECIMAL(10,2) = dbo.fn_ThamSoSo('UPLOAD_MAX_MB', 10);
    IF @KichThuoc > (@MaxMB * 1024 * 1024)
        THROW 50400, N'Dung luong tep vuot qua gioi han toi da cho phep.', 1;

    INSERT INTO HinhAnh
    (
        TenFile, TenGoc, DuongDan, LoaiHinhAnh, DinhDang, KichThuoc,
        ChieuRong, ChieuCao, HashMD5, LaNhayCam, MoTa,
        MaHoSoThiThe, MaHoSoMatTich, MaTinBao, MaXacMinh,
        MaTaiKhoanTaiLen, ThoiGianTaiLen, TrangThai
    )
    VALUES
    (
        @TenFile, @TenGoc, @DuongDan, @LoaiHinhAnh, @DinhDang, @KichThuoc,
        @ChieuRong, @ChieuCao, @HashMD5, @LaNhayCam, @MoTa,
        @MaHoSoThiThe, @MaHoSoMatTich, @MaTinBao, @MaXacMinh,
        @MaTaiKhoanTaiLen, SYSDATETIME(), N'HoatDong'
    );

    SET @MaHinhAnhMoi = SCOPE_IDENTITY();
    INSERT INTO NhatKyHeThong (HanhDong, DoiTuongTacDong, MaTaiKhoan)
    VALUES (CONCAT(N'TAI_ANH#', @MaHinhAnhMoi, N' (', @LoaiHinhAnh, N')'), CONCAT(N'HinhAnh#', @MaHinhAnhMoi), @MaTaiKhoanTaiLen);
END
GO

-- Lay hinh anh theo ho so (co loc an anh nhay cam cho nguoi dan)
CREATE OR ALTER PROCEDURE sp_LayHinhAnhTheoHoSo
    @MaTaiKhoanTruyCap BIGINT,
    @MaHoSoThiThe BIGINT = NULL,
    @MaHoSoMatTich BIGINT = NULL,
    @MaTinBao INT = NULL,
    @MaXacMinh BIGINT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CoQuyenXemToanBo BIT = 0;
    IF fn_CoQuyen(@MaTaiKhoanTruyCap, 'BODY', 'VIEW') = 1 
       OR fn_CoQuyen(@MaTaiKhoanTruyCap, 'FILE', 'VIEW') = 1
    BEGIN
        SET @CoQuyenXemToanBo = 1;
    END

    SELECT 
        h.MaHinhAnh,
        h.TenGoc,
        CASE 
            WHEN h.LaNhayCam = 1 AND @CoQuyenXemToanBo = 0 THEN N'PRIVATE_PROTECTED_IMAGE'
            ELSE h.DuongDan
        END AS DuongDan,
        h.LoaiHinhAnh,
        h.DinhDang,
        h.KichThuoc,
        h.LaNhayCam,
        h.MoTa,
        h.ThoiGianTaiLen,
        tk.HoTen AS NguoiTaiLen
    FROM HinhAnh h
    JOIN TaiKhoanNguoiDung tk ON tk.MaTaiKhoan = h.MaTaiKhoanTaiLen
    WHERE h.TrangThai = N'HoatDong'
      AND (@MaHoSoThiThe IS NULL OR h.MaHoSoThiThe = @MaHoSoThiThe)
      AND (@MaHoSoMatTich IS NULL OR h.MaHoSoMatTich = @MaHoSoMatTich)
      AND (@MaTinBao IS NULL OR h.MaTinBao = @MaTinBao)
      AND (@MaXacMinh IS NULL OR h.MaXacMinh = @MaXacMinh)
      AND (@CoQuyenXemToanBo = 1 OR h.LaNhayCam = 0)
    ORDER BY h.ThoiGianTaiLen DESC;
END
GO

-- Xoa mem hinh anh
CREATE OR ALTER PROCEDURE sp_XoaHinhAnh
    @MaTaiKhoan BIGINT,
    @MaHinhAnh BIGINT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    DECLARE @NguoiTao BIGINT;
    DECLARE @TrangThai NVARCHAR(20);

    SELECT @NguoiTao = MaTaiKhoanTaiLen, @TrangThai = TrangThai
    FROM HinhAnh WHERE MaHinhAnh = @MaHinhAnh;

    IF @NguoiTao IS NULL
        THROW 50404, N'Khong tim thay hinh anh can xoa.', 1;

    IF @NguoiTao <> @MaTaiKhoan AND fn_CoQuyen(@MaTaiKhoan, 'FILE', 'DELETE') = 0
        THROW 50403, N'Tai khoan khong co quyen xoa hinh anh nay.', 1;

    UPDATE HinhAnh SET TrangThai = N'DaXoa' WHERE MaHinhAnh = @MaHinhAnh;

    INSERT INTO NhatKyHeThong (HanhDong, DoiTuongTacDong, MaTaiKhoan)
    VALUES (CONCAT(N'XOA_HINH_ANH#', @MaHinhAnh), CONCAT(N'HinhAnh#', @MaHinhAnh), @MaTaiKhoan);
END
GO