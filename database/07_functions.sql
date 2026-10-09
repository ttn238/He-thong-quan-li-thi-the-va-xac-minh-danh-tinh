USE QL_XACMINHTHITHE;
GO
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
 
/* ---------- 5.1 HAM HE THONG ---------- */

CREATE OR ALTER FUNCTION fn_ThamSoSo (@Ten VARCHAR(100), @MacDinh DECIMAL(10,2))
RETURNS DECIMAL(10,2)
AS
BEGIN
    RETURN COALESCE(
        TRY_CAST((SELECT GiaTri FROM ThamSoHeThong WHERE TenThamSo = @Ten) AS DECIMAL(10,2)),
        @MacDinh);
END
GO

-- Nguoi thao tac hien tai (cac thu tuc dat bang sp_set_session_context)
CREATE OR ALTER FUNCTION fn_NguoiThaoTac ()
RETURNS BIGINT
AS
BEGIN
    RETURN CAST(SESSION_CONTEXT(N'user_id') AS BIGINT);
END
GO

-- Kiem tra tai khoan (dang hoat dong) co quyen Resource + Action hay khong
CREATE OR ALTER FUNCTION fn_CoQuyen (@MaTaiKhoan BIGINT, @Resource VARCHAR(80), @Action VARCHAR(30))
RETURNS BIT
AS
BEGIN
    RETURN CASE WHEN EXISTS (
        SELECT 1
        FROM TaiKhoanNguoiDung tk
        JOIN VaiTro_Quyen vq ON vq.MaVaiTro = tk.MaVaiTro
        JOIN QuyenHeThong q  ON q.MaQuyen = vq.MaQuyen
        WHERE tk.MaTaiKhoan = @MaTaiKhoan
          AND tk.TrangThai = N'HoatDong'
          AND q.Resource = @Resource
          AND q.[Action] = @Action) THEN 1 ELSE 0 END;
END
GO

/* ---------- 5.2 HAM TIEN ICH ---------- */

CREATE OR ALTER FUNCTION fn_TinhTuoi (@NgaySinh DATE, @TaiNgay DATE)
RETURNS INT
AS
BEGIN
    IF @NgaySinh IS NULL RETURN NULL;
    SET @TaiNgay = ISNULL(@TaiNgay, CAST(SYSDATETIME() AS DATE));
    RETURN DATEDIFF(YEAR, @NgaySinh, @TaiNgay)
         - CASE WHEN DATEADD(YEAR, DATEDIFF(YEAR, @NgaySinh, @TaiNgay), @NgaySinh) > @TaiNgay THEN 1 ELSE 0 END;
END
GO

-- Chuan hoa bien so: bo khoang trang, dau - . _ / , va viet hoa
CREATE OR ALTER FUNCTION fn_ChuanHoaBienSo (@BienSo VARCHAR(50))
RETURNS VARCHAR(20)
AS
BEGIN
    DECLARE @s VARCHAR(50) = UPPER(ISNULL(@BienSo, ''));
    SET @s = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(@s, ' ', ''), '-', ''), '.', ''), '_', ''), '/', ''), ',', '');
    RETURN LEFT(@s, 20);
END
GO

/* ---------- 5.3 QUY TAC CHUYEN TRANG THAI ---------- */

CREATE OR ALTER FUNCTION fn_ChuyenTrangThaiHopLe (@DoiTuong VARCHAR(30), @Nguon NVARCHAR(30), @Dich NVARCHAR(30))
RETURNS BIT
AS
BEGIN
    RETURN CASE WHEN EXISTS (
        SELECT 1
        FROM (VALUES
            ('TIN_BAO', N'MoiTiepNhan',  N'DaTiepNhan'),
            ('TIN_BAO', N'MoiTiepNhan',  N'TuChoi'),
            ('TIN_BAO', N'MoiTiepNhan',  N'YeuCauBoSung'),
            ('TIN_BAO', N'YeuCauBoSung', N'MoiTiepNhan'),
            ('TIN_BAO', N'DaTiepNhan',   N'DangXacMinh'),
            ('TIN_BAO', N'DangXacMinh',  N'DaXuLy'),

            ('HO_SO_THI_THE', N'MoiTiepNhan',   N'DangLuuGiu'),
            ('HO_SO_THI_THE', N'MoiTiepNhan',   N'DangXacMinh'),
            ('HO_SO_THI_THE', N'DangLuuGiu',    N'DangXacMinh'),
            ('HO_SO_THI_THE', N'DangXacMinh',   N'DaXacDinh'),
            ('HO_SO_THI_THE', N'DangXacMinh',   N'TiepTucLuuGiu'),
            ('HO_SO_THI_THE', N'TiepTucLuuGiu', N'DangXacMinh'),
            ('HO_SO_THI_THE', N'DaXacDinh',     N'DaBanGiao'),
            ('HO_SO_THI_THE', N'DaBanGiao',     N'DongHoSo'),
            ('HO_SO_THI_THE', N'DongHoSo',      N'TiepTucLuuGiu'),

            ('HO_SO_MAT_TICH', N'DangTimKiem',   N'CoHoSoNghiVan'),
            ('HO_SO_MAT_TICH', N'DangTimKiem',   N'DaDong'),
            ('HO_SO_MAT_TICH', N'CoHoSoNghiVan', N'DangTimKiem'),
            ('HO_SO_MAT_TICH', N'CoHoSoNghiVan', N'DaTimThay'),
            ('HO_SO_MAT_TICH', N'DaTimThay',     N'DaDong'),
            ('HO_SO_MAT_TICH', N'DaDong',        N'DangTimKiem'),

            ('DOI_SANH', N'ChoXuLy',      N'ChuyenXacMinh'),
            ('DOI_SANH', N'ChoXuLy',      N'LoaiBo'),
            ('DOI_SANH', N'ChuyenXacMinh', N'LoaiBo'),

            ('XAC_MINH', N'DangXacMinh', N'ChoDuyet'),
            ('XAC_MINH', N'DangXacMinh', N'KhongPhuHop'),
            ('XAC_MINH', N'ChoDuyet',    N'DaDuyet'),
            ('XAC_MINH', N'ChoDuyet',    N'TuChoiDuyet'),
            ('XAC_MINH', N'TuChoiDuyet', N'ChoDuyet'),
            ('XAC_MINH', N'TuChoiDuyet', N'KhongPhuHop'),

            ('BAN_GIAO', N'ChoBanGiao',  N'DangKiemTra'),
            ('BAN_GIAO', N'ChoBanGiao',  N'Huy'),
            ('BAN_GIAO', N'DangKiemTra', N'DaGiao'),
            ('BAN_GIAO', N'DangKiemTra', N'Huy'),
            ('BAN_GIAO', N'DaGiao',      N'DaXacNhan')
        ) AS t (DoiTuong, Nguon, Dich)
        WHERE t.DoiTuong = @DoiTuong AND t.Nguon = @Nguon AND t.Dich = @Dich) THEN 1 ELSE 0 END;
END
GO

/* ---------- 5.4 HAM TINH DIEM DOI SANH (moi tieu chi 0..100, NULL neu thieu du lieu) ---------- */

-- Thong tin co ban: gioi tinh (60), tuoi (40)
CREATE OR ALTER FUNCTION fn_DiemCoBan (@MaHoSoThiThe BIGINT, @MaHoSoMatTich BIGINT)
RETURNS DECIMAL(5,2)
AS
BEGIN
    IF NOT EXISTS (SELECT 1 FROM HoSoThiThe WHERE MaHoSoThiThe = @MaHoSoThiThe)
       OR NOT EXISTS (SELECT 1 FROM HoSoNguoiMatTich WHERE MaHoSoMatTich = @MaHoSoMatTich)
        RETURN NULL;

    DECLARE @gtT NVARCHAR(10), @tuoiT SMALLINT, @gtM NVARCHAR(10), @ns DATE, @tg DATETIME2;
    DECLARE @num DECIMAL(18,4) = 0, @den DECIMAL(18,4) = 0, @s DECIMAL(10,2), @tuoiM INT, @d INT;

    SELECT @gtT = GioiTinh, @tuoiT = TuoiUocLuong FROM HoSoThiThe WHERE MaHoSoThiThe = @MaHoSoThiThe;
    SELECT @gtM = GioiTinh, @ns = NgaySinh, @tg = ThoiGianMatTich FROM HoSoNguoiMatTich WHERE MaHoSoMatTich = @MaHoSoMatTich;

    -- Gioi tinh: trung 100, khac 0, khong ro (Khac / KhongXacDinh) 50
    IF @gtT IS NOT NULL AND @gtM IS NOT NULL
    BEGIN
        SET @s = CASE WHEN @gtT IN (N'Khac', N'KhongXacDinh') OR @gtM = N'Khac' THEN 50
                      WHEN @gtT = @gtM THEN 100
                      ELSE 0 END;
        SET @num += 60 * @s;
        SET @den += 60;
    END

    -- Tuoi: tuoi cua nguoi mat tich tai thoi diem mat tich so voi tuoi uoc luong cua thi the
    SET @tuoiM = fn_TinhTuoi(@ns, CAST(ISNULL(@tg, SYSDATETIME()) AS DATE));
    IF @tuoiM IS NOT NULL AND @tuoiT IS NOT NULL
    BEGIN
        SET @d = ABS(@tuoiM - @tuoiT);
        SET @s = CASE WHEN @d <= 3 THEN 100
                      WHEN 100 - 10 * (@d - 3) < 0 THEN 0
                      ELSE 100 - 10 * (@d - 3) END;
        SET @num += 40 * @s;
        SET @den += 40;
    END

    IF @den = 0 RETURN NULL;
    RETURN ROUND(@num / @den, 2);
END
GO

-- Dia diem (van ban tu do): trung nhau 100, cai nay chua cai kia 70, khac 0. So sanh khong phan biet hoa/thuong va dau
CREATE OR ALTER FUNCTION fn_DiemDiaDiem (@MaHoSoThiThe BIGINT, @MaHoSoMatTich BIGINT)
RETURNS DECIMAL(5,2)
AS
BEGIN
    DECLARE @a NVARCHAR(255), @b NVARCHAR(255);
    SELECT @a = DiaDiem FROM HoSoThiThe WHERE MaHoSoThiThe = @MaHoSoThiThe;
    SELECT @b = NoiMatTich FROM HoSoNguoiMatTich WHERE MaHoSoMatTich = @MaHoSoMatTich;

    SET @a = LTRIM(RTRIM(@a));
    SET @b = LTRIM(RTRIM(@b));
    IF @a IS NULL OR @b IS NULL OR @a = N'' OR @b = N'' RETURN NULL;

    IF @a = @b COLLATE Vietnamese_CI_AI RETURN 100;
    IF @a COLLATE Vietnamese_CI_AI LIKE N'%' + @b + N'%'
       OR @b COLLATE Vietnamese_CI_AI LIKE N'%' + @a + N'%'
        RETURN 70;
    RETURN 0;
END
GO

-- Thoi gian: thi the phai duoc phat hien SAU lan mat tich; cang gan diem cang cao
CREATE OR ALTER FUNCTION fn_DiemThoiGian (@MaHoSoThiThe BIGINT, @MaHoSoMatTich BIGINT)
RETURNS DECIMAL(5,2)
AS
BEGIN
    DECLARE @f DATETIME2, @l DATETIME2, @d DECIMAL(18,4);
    SELECT @f = ThoiGianPhatHien FROM HoSoThiThe WHERE MaHoSoThiThe = @MaHoSoThiThe;
    SELECT @l = ThoiGianMatTich  FROM HoSoNguoiMatTich WHERE MaHoSoMatTich = @MaHoSoMatTich;
    IF @f IS NULL OR @l IS NULL RETURN NULL;

    SET @d = DATEDIFF(SECOND, @l, @f) / 86400.0;
    RETURN CASE WHEN @d < 0 THEN 0
                WHEN @d <= 7 THEN 100
                WHEN @d <= 30 THEN 70
                WHEN @d <= 180 THEN 40
                ELSE 10 END;
END
GO

-- Tong diem co trong so (trong ThamSoHeThong, tong = 100). Tieu chi NULL bi bo ra va chia lai
CREATE OR ALTER FUNCTION fn_TongDiem (@DiemCoBan DECIMAL(5,2), @DiemDiaDiem DECIMAL(5,2), @DiemThoiGian DECIMAL(5,2))
RETURNS DECIMAL(5,2)
AS
BEGIN
    DECLARE @num DECIMAL(18,4) = 0, @den DECIMAL(18,4) = 0, @w DECIMAL(10,2);

    IF @DiemCoBan IS NOT NULL
    BEGIN SET @w = fn_ThamSoSo('MATCH_W_BASIC', 50);    SET @num += @w * @DiemCoBan;    SET @den += @w; END
    IF @DiemDiaDiem IS NOT NULL
    BEGIN SET @w = fn_ThamSoSo('MATCH_W_LOCATION', 30); SET @num += @w * @DiemDiaDiem;  SET @den += @w; END
    IF @DiemThoiGian IS NOT NULL
    BEGIN SET @w = fn_ThamSoSo('MATCH_W_TIME', 20);     SET @num += @w * @DiemThoiGian; SET @den += @w; END

    IF @den = 0 RETURN NULL;
    RETURN ROUND(@num / @den, 2);
END
GO

-- Muc do phu hop theo nguong cau hinh
CREATE OR ALTER FUNCTION fn_MucDo (@Diem DECIMAL(5,2))
RETURNS NVARCHAR(30)
AS
BEGIN
    IF @Diem IS NULL RETURN NULL;
    RETURN CASE WHEN @Diem > fn_ThamSoSo('MATCH_T_PRIORITY', 90) THEN N'UuTien'
                WHEN @Diem > fn_ThamSoSo('MATCH_T_VERIFY', 75)   THEN N'CanXacMinh'
                WHEN @Diem > fn_ThamSoSo('MATCH_T_POSSIBLE', 50) THEN N'CoKhaNang'
                ELSE N'Thap' END;
END
GO

/* ---------- 5.5 TRA CUU PHUONG TIEN THEO BIEN SO ----------
   Khop chinh xac truoc, sau do gan dung (cung do dai, sai toi da 1 ky tu - OCR doc nham) */
CREATE OR ALTER FUNCTION fn_TimPhuongTien (@BienSo VARCHAR(50))
RETURNS TABLE
AS
RETURN
    SELECT pt.MaPhuongTien, pt.BienSo, pt.LoaiXe, pt.NhanHieu, pt.MauSac, pt.ChuXe,
           CASE WHEN x.Chuan = p.Chuan THEN CAST(100 AS DECIMAL(5,2))
                ELSE CAST(100.0 * (LEN(p.Chuan) - d.Khac) / NULLIF(LEN(p.Chuan), 0) AS DECIMAL(5,2))
           END AS DoGiongNhau
    FROM (SELECT fn_ChuanHoaBienSo(@BienSo) AS Chuan) p
    CROSS JOIN PhuongTien pt
    CROSS APPLY (SELECT fn_ChuanHoaBienSo(pt.BienSo) AS Chuan) x
    CROSS APPLY (SELECT COUNT(*) AS Khac
                 FROM (VALUES (1),(2),(3),(4),(5),(6),(7),(8),(9),(10),(11),(12),(13),(14),(15)) n(i)
                 WHERE n.i <= LEN(p.Chuan)
                   AND SUBSTRING(x.Chuan, n.i, 1) <> SUBSTRING(p.Chuan, n.i, 1)) d
    WHERE LEN(p.Chuan) > 0
      AND (x.Chuan = p.Chuan OR (LEN(x.Chuan) = LEN(p.Chuan) AND d.Khac <= 1));
GO