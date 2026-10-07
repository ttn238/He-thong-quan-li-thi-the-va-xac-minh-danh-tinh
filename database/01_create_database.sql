USE master;
GO
  
IF DB_ID('QL_XACMINHTHITHE') IS NOT NULL
BEGIN
    ALTER DATABASE QL_XACMINHTHITHE
    SET SINGLE_USER WITH ROLLBACK IMMEDIATE;

    DROP DATABASE QL_XACMINHTHITHE;
END
GO

CREATE DATABASE QL_XACMINHTHITHE;
GO

USE QL_XACMINHTHITHE;
GO
 
CREATE TABLE CauHinhHeThong
(
    MaCauHinh INT IDENTITY(1,1) PRIMARY KEY,
    TenCauHinh NVARCHAR(100) NOT NULL UNIQUE,
    GiaTri NVARCHAR(500) NULL,
    MoTa NVARCHAR(500) NULL,
    TrangThai BIT NOT NULL DEFAULT 1,
    NgayCapNhat DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);
GO
 
CREATE TABLE VaiTro
(
    MaVaiTro INT IDENTITY(1,1) PRIMARY KEY,
    TenVaiTro NVARCHAR(50) NOT NULL UNIQUE,
    MoTa NVARCHAR(255),
    TrangThai BIT NOT NULL DEFAULT 1
);
GO
 
CREATE TABLE Quyen
(
    MaQuyen INT IDENTITY(1,1) PRIMARY KEY,
    TenQuyen NVARCHAR(100) NOT NULL UNIQUE,
    MaChucNang NVARCHAR(100) NOT NULL,
    MoTa NVARCHAR(255)
);
GO
 
CREATE TABLE VaiTro_Quyen
(
    MaVaiTro INT NOT NULL,
    MaQuyen INT NOT NULL,

    PRIMARY KEY (MaVaiTro, MaQuyen),

    CONSTRAINT FK_VaiTroQuyen_VaiTro
        FOREIGN KEY (MaVaiTro)
        REFERENCES VaiTro(MaVaiTro),

    CONSTRAINT FK_VaiTroQuyen_Quyen
        FOREIGN KEY (MaQuyen)
        REFERENCES Quyen(MaQuyen)
);
GO
 
CREATE TABLE DonVi
(
    MaDonVi INT IDENTITY(1,1) PRIMARY KEY,
    TenDonVi NVARCHAR(200) NOT NULL,
    LoaiDonVi NVARCHAR(50),
    DiaChi NVARCHAR(500),
    SoDienThoai VARCHAR(20),
    Email VARCHAR(100),
    TrangThai BIT NOT NULL DEFAULT 1
);
GO
 
CREATE TABLE NguoiDung
(
    MaNguoiDung INT IDENTITY(1,1) PRIMARY KEY,

    TenDangNhap VARCHAR(50) NOT NULL UNIQUE,

    MatKhauHash VARCHAR(255) NOT NULL,

    HoTen NVARCHAR(150) NOT NULL,

    Email VARCHAR(100),

    SoDienThoai VARCHAR(20),

    MaVaiTro INT NOT NULL,

    MaDonVi INT NULL,

    TrangThai BIT NOT NULL DEFAULT 1,

    LanDangNhapCuoi DATETIME2 NULL,

    NgayTao DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT FK_NguoiDung_VaiTro
        FOREIGN KEY (MaVaiTro)
        REFERENCES VaiTro(MaVaiTro),

    CONSTRAINT FK_NguoiDung_DonVi
        FOREIGN KEY (MaDonVi)
        REFERENCES DonVi(MaDonVi)
);
GO

CREATE TABLE TinBao (
    MaTinBao INT
);

CREATE TABLE HoSoThiThe (
    MaThiThe INT
);

CREATE TABLE HinhAnhThiThe (
    MaHinhAnh INT
);

CREATE TABLE DoVat (
    MaDoVat INT
);

CREATE TABLE ViTriLuuTru (
    MaViTri INT
);

CREATE TABLE NganLuuTru (
    MaNgan INT
);

CREATE TABLE HoSoNguoiMatTich (
    MaNguoiMatTich INT
);

CREATE TABLE HinhAnhNguoiMatTich (
    MaHinhAnh INT
);

CREATE TABLE NguoiThan (
    MaNguoiThan INT
);

CREATE TABLE KetQuaOCR (
    MaOCR INT
);

CREATE TABLE BienSoXe (
    MaBienSo INT
);

CREATE TABLE KetQuaDoiSanh (
    MaDoiSanh INT
);

CREATE TABLE ChiTietDoiSanh (
    MaChiTietDoiSanh INT
);

CREATE TABLE HoSoXacMinh (
    MaXacMinh INT
);

CREATE TABLE KetQuaXacMinh (
    MaKetQua INT
);

CREATE TABLE NguoiXacMinh (
    MaNguoiXacMinh INT
);

CREATE TABLE KetLuan (
    MaKetLuan INT
);

CREATE TABLE PheDuyetKetLuan (
    MaPheDuyet INT
);

CREATE TABLE BanGiao (
    MaBanGiao INT
);

CREATE TABLE ChiTietBanGiao (
    MaChiTietBanGiao INT
);

CREATE TABLE NhatKyHeThong (
    MaNhatKy INT
);

CREATE TABLE ThongBao (
    MaThongBao INT
);