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