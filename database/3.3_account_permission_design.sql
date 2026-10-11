USE QL_XACMINHTHITHE;
GO
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

/* =========================================================================
   PHẦN 3.3: THIẾT KẾ ACCOUNT / PERMISSION (TÀI KHOẢN & PHÂN QUYỀN)
   Tác giả phụ trách: Hữu Khoa (Branch: B-HuuKhoa)
   Mục đích:
     - Quản lý tài khoản người dùng, cán bộ, quản trị viên
     - Quản lý vai trò (Role), quyền hạn (Permission) theo mô hình RBAC
     - Cơ chế chống vét cạn Brute-force: Khóa tạm sau 5 lần đăng nhập sai
     - Lưu vết lịch sử đăng nhập phục vụ an toàn hệ thống
   ========================================================================= */

/* 1. ĐẢM BẢO CÁC CỘT AN NINH CHO BẢNG TaiKhoanNguoiDung */
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'TaiKhoanNguoiDung') AND name = N'CCCD')
BEGIN
    ALTER TABLE TaiKhoanNguoiDung ADD CCCD VARCHAR(20) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'TaiKhoanNguoiDung') AND name = N'ChucVu')
BEGIN
    ALTER TABLE TaiKhoanNguoiDung ADD ChucVu NVARCHAR(100) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'TaiKhoanNguoiDung') AND name = N'SoLanDangNhapSai')
BEGIN
    ALTER TABLE TaiKhoanNguoiDung ADD SoLanDangNhapSai INT NOT NULL CONSTRAINT DF_TaiKhoan_SoLanSai DEFAULT 0;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'TaiKhoanNguoiDung') AND name = N'KhoaDenThoiDiem')
BEGIN
    ALTER TABLE TaiKhoanNguoiDung ADD KhoaDenThoiDiem DATETIME2 NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'TaiKhoanNguoiDung') AND name = N'LanDangNhapCuoi')
BEGIN
    ALTER TABLE TaiKhoanNguoiDung ADD LanDangNhapCuoi DATETIME2 NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'TaiKhoanNguoiDung') AND name = N'NgayTao')
BEGIN
    ALTER TABLE TaiKhoanNguoiDung ADD NgayTao DATETIME2 NOT NULL CONSTRAINT DF_TaiKhoan_NgayTao DEFAULT SYSDATETIME();
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'TaiKhoanNguoiDung') AND name = N'NgayCapNhat')
BEGIN
    ALTER TABLE TaiKhoanNguoiDung ADD NgayCapNhat DATETIME2 NOT NULL CONSTRAINT DF_TaiKhoan_NgayCapNhat DEFAULT SYSDATETIME();
END
GO

/* 2. CHỈ MỤC DUY NHẤT CHO CCCD */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_TaiKhoan_CCCD' AND object_id = OBJECT_ID(N'TaiKhoanNguoiDung'))
BEGIN
    CREATE UNIQUE INDEX UQ_TaiKhoan_CCCD ON TaiKhoanNguoiDung(CCCD) WHERE CCCD IS NOT NULL;
END
GO

/* 3. BẢNG GHI LỊCH SỬ ĐĂNG NHẬP */
IF OBJECT_ID(N'LichSuDangNhap', N'U') IS NULL
BEGIN
    CREATE TABLE LichSuDangNhap
    (
        MaLichSu BIGINT IDENTITY(1,1) PRIMARY KEY,
        TenDangNhap VARCHAR(50) NOT NULL,
        MaTaiKhoan BIGINT NULL,
        ThoiGian DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
        DiaChiIP VARCHAR(45) NULL,
        UserAgent NVARCHAR(500) NULL,
        ThanhCong BIT NOT NULL,
        LyDoThatBai NVARCHAR(255) NULL,

        CONSTRAINT FK_LichSuDangNhap_TaiKhoan
            FOREIGN KEY (MaTaiKhoan) REFERENCES TaiKhoanNguoiDung(MaTaiKhoan)
            ON DELETE SET NULL
    );

    CREATE INDEX IX_LichSuDangNhap_ThoiGian ON LichSuDangNhap(ThoiGian DESC);
    CREATE INDEX IX_LichSuDangNhap_TaiKhoan ON LichSuDangNhap(MaTaiKhoan);
END
GO

/* 4. STORED PROCEDURES QUẢN LÝ TÀI KHOẢN & XÁC THỰC */

-- 4.1. ĐĂNG NHẬP & BẢO VỆ CHỐNG BRUTE-FORCE
CREATE OR ALTER PROCEDURE sp_DangNhap
    @TenDangNhap VARCHAR(50),
    @DiaChiIP VARCHAR(45) = NULL,
    @UserAgent NVARCHAR(500) = NULL,
    @MatKhauHopLe BIT, -- Kết quả verify băm bcrypt từ backend API (1: khớp, 0: sai)
    @KetQua INT OUTPUT, -- 1: Thành công, -1: Tài khoản không tồn tại, -2: Bị khóa vĩnh viễn, -3: Đang bị khóa tạm
    @ThongBao NVARCHAR(255) OUTPUT,
    @MaTaiKhoan BIGINT OUTPUT,
    @MaVaiTro INT OUTPUT,
    @HoTen NVARCHAR(150) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

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

    -- Kiểm tra tồn tại
    IF @MaTaiKhoan IS NULL
    BEGIN
        SET @KetQua = -1;
        SET @ThongBao = N'Tài khoản hoặc mật khẩu không chính xác';
        INSERT INTO LichSuDangNhap (TenDangNhap, MaTaiKhoan, DiaChiIP, UserAgent, ThanhCong, LyDoThatBai)
        VALUES (@TenDangNhap, NULL, @DiaChiIP, @UserAgent, 0, N'Tài khoản không tồn tại');
        RETURN;
    END

    -- Kiểm tra tài khoản bị khóa vĩnh viễn
    IF @TrangThai = N'Khoa'
    BEGIN
        SET @KetQua = -2;
        SET @ThongBao = N'Tài khoản đã bị khóa bởi Quản trị viên. Vui lòng liên hệ hỗ trợ.';
        INSERT INTO LichSuDangNhap (TenDangNhap, MaTaiKhoan, DiaChiIP, UserAgent, ThanhCong, LyDoThatBai)
        VALUES (@TenDangNhap, @MaTaiKhoan, @DiaChiIP, @UserAgent, 0, N'Tài khoản bị khóa');
        RETURN;
    END

    -- Kiểm tra tài khoản có đang bị khóa tạm thời do sai quá số lần không
    IF @KhoaDen IS NOT NULL AND @KhoaDen > @HienTai
    BEGIN
        SET @KetQua = -3;
        SET @ThongBao = CONCAT(N'Tài khoản đang bị tạm khóa đến ', FORMAT(@KhoaDen, 'HH:mm:ss dd/MM/yyyy'), N' do nhập sai quá 5 lần.');
        INSERT INTO LichSuDangNhap (TenDangNhap, MaTaiKhoan, DiaChiIP, UserAgent, ThanhCong, LyDoThatBai)
        VALUES (@TenDangNhap, @MaTaiKhoan, @DiaChiIP, @UserAgent, 0, N'Đang trong thời gian khóa tạm');
        RETURN;
    END

    -- Nếu đã hết thời gian khóa tạm thì tự động mở lại
    IF @KhoaDen IS NOT NULL AND @KhoaDen <= @HienTai AND @TrangThai = N'KhoaTam'
    BEGIN
        UPDATE TaiKhoanNguoiDung
        SET TrangThai = N'HoatDong',
            SoLanDangNhapSai = 0,
            KhoaDenThoiDiem = NULL
        WHERE MaTaiKhoan = @MaTaiKhoan;
        SET @SoLanSai = 0;
    END

    -- Kiểm tra mật khẩu
    IF @MatKhauHopLe = 1
    BEGIN
        -- Đăng nhập thành công: Reset bộ đếm lỗi, cập nhật thời gian
        UPDATE TaiKhoanNguoiDung
        SET SoLanDangNhapSai = 0,
            KhoaDenThoiDiem = NULL,
            LanDangNhapCuoi = @HienTai
        WHERE MaTaiKhoan = @MaTaiKhoan;

        SET @KetQua = 1;
        SET @ThongBao = N'Đăng nhập thành công';

        INSERT INTO LichSuDangNhap (TenDangNhap, MaTaiKhoan, DiaChiIP, UserAgent, ThanhCong, LyDoThatBai)
        VALUES (@TenDangNhap, @MaTaiKhoan, @DiaChiIP, @UserAgent, 1, NULL);

        -- Ghi nhật ký hệ thống
        INSERT INTO NhatKyHeThong (HanhDong, DoiTuongTacDong, MaTaiKhoan)
        VALUES (N'Đăng nhập thành công', CONCAT(N'TaiKhoan#', @MaTaiKhoan), @MaTaiKhoan);
    END
    ELSE
    BEGIN
        -- Mật khẩu không đúng: Tăng số lần sai
        SET @SoLanSai = @SoLanSai + 1;
        IF @SoLanSai >= 5
        BEGIN
            -- Khóa tạm 15 phút
            SET @KhoaDen = DATEADD(MINUTE, 15, @HienTai);
            UPDATE TaiKhoanNguoiDung
            SET SoLanDangNhapSai = @SoLanSai,
                TrangThai = N'KhoaTam',
                KhoaDenThoiDiem = @KhoaDen
            WHERE MaTaiKhoan = @MaTaiKhoan;

            SET @KetQua = -3;
            SET @ThongBao = N'Bạn đã nhập sai mật khẩu 5 lần liên tiếp. Tài khoản bị tạm khóa 15 phút.';
        END
        ELSE
        BEGIN
            UPDATE TaiKhoanNguoiDung
            SET SoLanDangNhapSai = @SoLanSai
            WHERE MaTaiKhoan = @MaTaiKhoan;

            SET @KetQua = 0;
            SET @ThongBao = CONCAT(N'Mật khẩu không chính xác. Bạn còn ', 5 - @SoLanSai, N' lần thử.');
        END

        INSERT INTO LichSuDangNhap (TenDangNhap, MaTaiKhoan, DiaChiIP, UserAgent, ThanhCong, LyDoThatBai)
        VALUES (@TenDangNhap, @MaTaiKhoan, @DiaChiIP, @UserAgent, 0, N'Sai mật khẩu');
    END
END
GO

-- 4.2. ĐĂNG KÝ TÀI KHOẢN NGƯỜI DÂN
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
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    -- Kiểm tra trùng tên đăng nhập
    IF EXISTS (SELECT 1 FROM TaiKhoanNguoiDung WHERE TenDangNhap = @TenDangNhap)
        THROW 50409, N'Tên đăng nhập này đã tồn tại trên hệ thống.', 1;

    -- Kiểm tra trùng Email
    IF @Email IS NOT NULL AND EXISTS (SELECT 1 FROM TaiKhoanNguoiDung WHERE Email = @Email)
        THROW 50409, N'Địa chỉ Email này đã được sử dụng.', 1;

    -- Kiểm tra trùng Số điện thoại
    IF @SoDienThoai IS NOT NULL AND EXISTS (SELECT 1 FROM TaiKhoanNguoiDung WHERE SoDienThoai = @SoDienThoai)
        THROW 50409, N'Số điện thoại này đã được sử dụng.', 1;

    -- Kiểm tra trùng CCCD
    IF @CCCD IS NOT NULL AND EXISTS (SELECT 1 FROM TaiKhoanNguoiDung WHERE CCCD = @CCCD)
        THROW 50409, N'Số CCCD này đã tồn tại trong hệ thống.', 1;

    -- Lấy mã vai trò NGUOI_DAN mặc định
    DECLARE @MaVaiTro INT;
    SELECT @MaVaiTro = MaVaiTro FROM VaiTro WHERE TenVaiTro LIKE N'%Người dân%' OR TenVaiTro = N'NGUOI_DAN';

    IF @MaVaiTro IS NULL
        SELECT TOP 1 @MaVaiTro = MaVaiTro FROM VaiTro WHERE MaVaiTro = 5; -- Fallback id

    INSERT INTO TaiKhoanNguoiDung (TenDangNhap, MatKhauHash, HoTen, Email, SoDienThoai, CCCD, MaVaiTro, TrangThai, NgayTao)
    VALUES (@TenDangNhap, @MatKhauHash, @HoTen, @Email, @SoDienThoai, @CCCD, @MaVaiTro, N'HoatDong', SYSDATETIME());

    SET @MaTaiKhoanMoi = SCOPE_IDENTITY();

    -- Ghi nhật ký
    INSERT INTO NhatKyHeThong (HanhDong, DoiTuongTacDong, MaTaiKhoan)
    VALUES (N'Đăng ký tài khoản', CONCAT(N'TaiKhoan#', @MaTaiKhoanMoi), @MaTaiKhoanMoi);
END
GO

-- 4.3. ĐỔI MẬT KHẨU TÀI KHOẢN
CREATE OR ALTER PROCEDURE sp_DoiMatKhau
    @MaTaiKhoan BIGINT,
    @MatKhauMoiHash VARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM TaiKhoanNguoiDung WHERE MaTaiKhoan = @MaTaiKhoan)
        THROW 50404, N'Không tìm thấy tài khoản cần đổi mật khẩu.', 1;

    UPDATE TaiKhoanNguoiDung
    SET MatKhauHash = @MatKhauMoiHash,
        NgayCapNhat = SYSDATETIME()
    WHERE MaTaiKhoan = @MaTaiKhoan;

    INSERT INTO NhatKyHeThong (HanhDong, DoiTuongTacDong, MaTaiKhoan)
    VALUES (N'Đổi mật khẩu', CONCAT(N'TaiKhoan#', @MaTaiKhoan), @MaTaiKhoan);
END
GO

-- 4.4. KHÓA / MỞ KHÓA TÀI KHOẢN (DÀNH CHO ADMIN)
CREATE OR ALTER PROCEDURE sp_KhoaMoKhoaTaiKhoan
    @MaTaiKhoanAdmin BIGINT,
    @MaTaiKhoanMucTieu BIGINT,
    @TrangThaiMoi NVARCHAR(20), -- 'HoatDong', 'Khoa'
    @LyDo NVARCHAR(255) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF fn_CoQuyen(@MaTaiKhoanAdmin, 'ACCOUNT', 'UPDATE') = 0
        THROW 50403, N'Bạn không có quyền quản lý trạng thái tài khoản.', 1;

    IF @TrangThaiMoi NOT IN (N'HoatDong', N'Khoa')
        THROW 50400, N'Trạng thái cập nhật không hợp lệ.', 1;

    IF @MaTaiKhoanAdmin = @MaTaiKhoanMucTieu AND @TrangThaiMoi = N'Khoa'
        THROW 50400, N'Quản trị viên không thể tự khóa tài khoản của chính mình.', 1;

    UPDATE TaiKhoanNguoiDung
    SET TrangThai = @TrangThaiMoi,
        SoLanDangNhapSai = 0,
        KhoaDenThoiDiem = NULL,
        NgayCapNhat = SYSDATETIME()
    WHERE MaTaiKhoan = @MaTaiKhoanMucTieu;

    INSERT INTO NhatKyHeThong (HanhDong, DoiTuongTacDong, MaTaiKhoan)
    VALUES (CONCAT(N'Đổi trạng thái tài khoản sang ', @TrangThaiMoi, N': ', ISNULL(@LyDo, N'')),
            CONCAT(N'TaiKhoan#', @MaTaiKhoanMucTieu),
            @MaTaiKhoanAdmin);
END
GO

-- 4.5. PHÂN QUYỀN VAI TRÒ (DÀNH CHO ADMIN)
CREATE OR ALTER PROCEDURE sp_PhanQuyenVaiTro
    @MaTaiKhoanAdmin BIGINT,
    @MaVaiTro INT,
    @MaQuyen INT,
    @PhamVi VARCHAR(10) = 'ALL',
    @HanhDong VARCHAR(10) -- 'GAN' (gán quyền) hoặc 'HUY' (thu hồi quyền)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF fn_CoQuyen(@MaTaiKhoanAdmin, 'PERMISSION', 'UPDATE') = 0
        THROW 50403, N'Bạn không có quyền phân quyền vai trò.', 1;

    IF @HanhDong = 'GAN'
    BEGIN
        IF EXISTS (SELECT 1 FROM VaiTro_Quyen WHERE MaVaiTro = @MaVaiTro AND MaQuyen = @MaQuyen)
        BEGIN
            UPDATE VaiTro_Quyen SET PhamVi = @PhamVi WHERE MaVaiTro = @MaVaiTro AND MaQuyen = @MaQuyen;
        END
        ELSE
        BEGIN
            INSERT INTO VaiTro_Quyen (MaVaiTro, MaQuyen, PhamVi) VALUES (@MaVaiTro, @MaQuyen, @PhamVi);
        END
    END
    ELSE IF @HanhDong = 'HUY'
    BEGIN
        DELETE FROM VaiTro_Quyen WHERE MaVaiTro = @MaVaiTro AND MaQuyen = @MaQuyen;
    END

    INSERT INTO NhatKyHeThong (HanhDong, DoiTuongTacDong, MaTaiKhoan)
    VALUES (CONCAT(@HanhDong, N' quyền #', @MaQuyen, N' cho vai trò #', @MaVaiTro),
            N'VaiTro_Quyen',
            @MaTaiKhoanAdmin);
END
GO
