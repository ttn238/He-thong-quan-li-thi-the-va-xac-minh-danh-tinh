USE QL_XACMINHTHITHE;
GO
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

/* =========================================================================
   PHẦN 3.7: THIẾT KẾ BẢNG HINHANH (QUẢN LÝ HÌNH ẢNH & TỆP ĐÍNH KÈM)
   Tác giả phụ trách: Hữu Khoa (Branch: B-HuuKhoa)
   Mục đích:
     - Quản lý tập trung toàn bộ hình ảnh trong hệ thống
     - Phân định rõ ràng ảnh thi thể, ảnh người mất tích, hiện trường, minh chứng ADN
     - Cơ chế bảo vệ ảnh nhạy cảm tử thi (LaNhayCam = 1) theo yêu cầu NFR1.2
     - Tích hợp liên kết với AI Face Matching và OCR biển số
   ========================================================================= */

/* 1. TẠO BẢNG HinhAnh */
IF OBJECT_ID(N'HinhAnh', N'U') IS NULL
BEGIN
    CREATE TABLE HinhAnh
    (
        MaHinhAnh BIGINT IDENTITY(1,1) PRIMARY KEY,
        TenFile NVARCHAR(255) NOT NULL,
        TenGoc NVARCHAR(255) NOT NULL,
        DuongDan NVARCHAR(500) NOT NULL,
        LoaiHinhAnh NVARCHAR(50) NOT NULL,
        DinhDang VARCHAR(30) NOT NULL,
        KichThuoc BIGINT NOT NULL,
        ChieuRong INT NULL,
        ChieuCao INT NULL,
        HashMD5 VARCHAR(64) NULL,
        LaNhayCam BIT NOT NULL DEFAULT 0,
        MoTa NVARCHAR(500) NULL,

        -- Các khóa ngoại liên kết đa tượng
        MaHoSoThiThe BIGINT NULL,
        MaHoSoMatTich BIGINT NULL,
        MaTinBao INT NULL,
        MaXacMinh BIGINT NULL,

        MaTaiKhoanTaiLen BIGINT NOT NULL,
        ThoiGianTaiLen DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
        TrangThai NVARCHAR(20) NOT NULL DEFAULT N'HoatDong',

        -- Ràng buộc khóa ngoại
        CONSTRAINT FK_HinhAnh_HoSoThiThe
            FOREIGN KEY (MaHoSoThiThe) REFERENCES HoSoThiThe(MaHoSoThiThe)
            ON DELETE NO ACTION,

        CONSTRAINT FK_HinhAnh_HoSoMatTich
            FOREIGN KEY (MaHoSoMatTich) REFERENCES HoSoNguoiMatTich(MaHoSoMatTich)
            ON DELETE NO ACTION,

        CONSTRAINT FK_HinhAnh_TinBao
            FOREIGN KEY (MaTinBao) REFERENCES TinBao(MaTinBao)
            ON DELETE NO ACTION,

        CONSTRAINT FK_HinhAnh_XacMinh
            FOREIGN KEY (MaXacMinh) REFERENCES XacMinh(MaXacMinh)
            ON DELETE NO ACTION,

        CONSTRAINT FK_HinhAnh_TaiKhoan
            FOREIGN KEY (MaTaiKhoanTaiLen) REFERENCES TaiKhoanNguoiDung(MaTaiKhoan),

        -- Ràng buộc kiểm tra
        CONSTRAINT CK_HinhAnh_KichThuoc
            CHECK (KichThuoc > 0),

        CONSTRAINT CK_HinhAnh_TrangThai
            CHECK (TrangThai IN (N'HoatDong', N'DaXoa', N'An')),

        -- Một ảnh bắt buộc phải thuộc về ít nhất 1 hồ sơ / đối tượng nghiệp vụ
        CONSTRAINT CK_HinhAnh_ThuocDoiTuong
            CHECK (MaHoSoThiThe IS NOT NULL 
                OR MaHoSoMatTich IS NOT NULL 
                OR MaTinBao IS NOT NULL 
                OR MaXacMinh IS NOT NULL),

        -- Quy tắc bảo mật: Mọi ảnh thuộc hồ sơ tử thi đều bắt buộc là ảnh nhạy cảm
        CONSTRAINT CK_HinhAnh_ThiThe_NhayCam
            CHECK (MaHoSoThiThe IS NULL OR LaNhayCam = 1)
    );
END
GO

/* 2. CHỈ MỤC TỐI ƯU HÓA TRUY VẤN (INDEXES) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_HinhAnh_HoSoThiThe' AND object_id = OBJECT_ID(N'HinhAnh'))
BEGIN
    CREATE INDEX IX_HinhAnh_HoSoThiThe ON HinhAnh(MaHoSoThiThe) WHERE TrangThai = N'HoatDong';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_HinhAnh_HoSoMatTich' AND object_id = OBJECT_ID(N'HinhAnh'))
BEGIN
    CREATE INDEX IX_HinhAnh_HoSoMatTich ON HinhAnh(MaHoSoMatTich) WHERE TrangThai = N'HoatDong';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_HinhAnh_TinBao' AND object_id = OBJECT_ID(N'HinhAnh'))
BEGIN
    CREATE INDEX IX_HinhAnh_TinBao ON HinhAnh(MaTinBao) WHERE TrangThai = N'HoatDong';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_HinhAnh_XacMinh' AND object_id = OBJECT_ID(N'HinhAnh'))
BEGIN
    CREATE INDEX IX_HinhAnh_XacMinh ON HinhAnh(MaXacMinh) WHERE TrangThai = N'HoatDong';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_HinhAnh_TaiKhoanTaiLen' AND object_id = OBJECT_ID(N'HinhAnh'))
BEGIN
    CREATE INDEX IX_HinhAnh_TaiKhoanTaiLen ON HinhAnh(MaTaiKhoanTaiLen);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_HinhAnh_KhongNhayCam' AND object_id = OBJECT_ID(N'HinhAnh'))
BEGIN
    CREATE INDEX IX_HinhAnh_KhongNhayCam ON HinhAnh(LaNhayCam, TrangThai);
END
GO

/* 3. STORED PROCEDURES THAO TÁC HÌNH ẢNH */

-- 3.1. THÊM MỚI HÌNH ẢNH VÀO HỆ THỐNG
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
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    -- Kiểm tra quyền tải file của tài khoản
    IF fn_CoQuyen(@MaTaiKhoanTaiLen, 'FILE', 'CREATE') = 0
        THROW 50403, N'Bạn không có quyền tải hình ảnh lên hệ thống.', 1;

    -- Nếu là ảnh thi thể, luôn ép cờ LaNhayCam = 1
    IF @MaHoSoThiThe IS NOT NULL
        SET @LaNhayCam = 1;

    -- Kiểm tra giới hạn dung lượng từ tham số hệ thống (mặc định 10MB)
    DECLARE @MaxMB DECIMAL(10,2) = dbo.fn_ThamSoSo('UPLOAD_MAX_MB', 10);
    IF @KichThuoc > (@MaxMB * 1024 * 1024)
        THROW 50400, N'Dung lượng tệp vượt quá giới hạn tối đa cho phép.', 1;

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

    -- Ghi nhật ký
    INSERT INTO NhatKyHeThong (HanhDong, DoiTuongTacDong, MaTaiKhoan)
    VALUES (CONCAT(N'Tải lên hình ảnh #', @MaHinhAnhMoi, N' (', @LoaiHinhAnh, N')'),
            CONCAT(N'HinhAnh#', @MaHinhAnhMoi),
            @MaTaiKhoanTaiLen);
END
GO

-- 3.2. TRUY VẤN HÌNH ẢNH THEO HỒ SƠ (CÓ LỌC BẢO MẬT ẢNH NHẠY CẢM)
CREATE OR ALTER PROCEDURE sp_LayHinhAnhTheoHoSo
    @MaTaiKhoanTruyCap BIGINT,
    @MaHoSoThiThe BIGINT = NULL,
    @MaHoSoMatTich BIGINT = NULL,
    @MaTinBao INT = NULL,
    @MaXacMinh BIGINT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Kiểm tra tài khoản truy cập có quyền xem toàn bộ hay chỉ xem ảnh công khai
    DECLARE @CoQuyenXemToanBo BIT = 0;

    -- Nếu là Cán bộ hoặc Admin thì có quyền xem ảnh nhạy cảm
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
      -- Nếu người dùng thông thường thì không hiển thị ảnh nhạy cảm tử thi
      AND (@CoQuyenXemToanBo = 1 OR h.LaNhayCam = 0)
    ORDER BY h.ThoiGianTaiLen DESC;
END
GO

-- 3.3. XÓA MỀM HÌNH ẢNH
CREATE OR ALTER PROCEDURE sp_XoaHinhAnh
    @MaTaiKhoan BIGINT,
    @MaHinhAnh BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @NguoiTao BIGINT;
    DECLARE @TrangThai NVARCHAR(20);

    SELECT @NguoiTao = MaTaiKhoanTaiLen, @TrangThai = TrangThai
    FROM HinhAnh
    WHERE MaHinhAnh = @MaHinhAnh;

    IF @NguoiTao IS NULL
        THROW 50404, N'Không tìm thấy hình ảnh cần xóa.', 1;

    -- Chỉ chủ sở hữu tệp hoặc Cán bộ/Admin có quyền FILE.DELETE mới được xóa
    IF @NguoiTao <> @MaTaiKhoan AND fn_CoQuyen(@MaTaiKhoan, 'FILE', 'DELETE') = 0
        THROW 50403, N'Bạn không có quyền xóa hình ảnh này.', 1;

    UPDATE HinhAnh
    SET TrangThai = N'DaXoa'
    WHERE MaHinhAnh = @MaHinhAnh;

    INSERT INTO NhatKyHeThong (HanhDong, DoiTuongTacDong, MaTaiKhoan)
    VALUES (CONCAT(N'Xóa hình ảnh #', @MaHinhAnh),
            CONCAT(N'HinhAnh#', @MaHinhAnh),
            @MaTaiKhoan);
END
GO
