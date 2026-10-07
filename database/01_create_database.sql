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


CREATE TABLE LoaiTinBao
(
    MaLoaiTinBao INT IDENTITY(1,1) PRIMARY KEY,
    TenLoaiTinBao NVARCHAR(100) NOT NULL UNIQUE,
    MoTa NVARCHAR(255) NULL,
    TrangThai BIT NOT NULL DEFAULT 1
);
GO

CREATE TABLE TinBao (
     MaTinBao INT IDENTITY(1,1) PRIMARY KEY,

    -- Ma hien thi, vi du TB000001
    MaTin AS ('TB' + RIGHT('000000' + CAST(MaTinBao AS VARCHAR(10)), 6)) PERSISTED,

    MaLoaiTinBao INT NOT NULL,

    TieuDe NVARCHAR(200) NOT NULL,
    NoiDung NVARCHAR(MAX) NOT NULL,

    ThoiGianXayRa DATETIME2 NOT NULL,
    DiaDiem NVARCHAR(500) NOT NULL,

    NguonTin NVARCHAR(20) NOT NULL DEFAULT N'NguoiDan',

    -- Thong tin nguoi bao tin (nguoi dan co the khong co tai khoan)
    HoTenNguoiBao NVARCHAR(150) NULL,
    SoDienThoaiNguoiBao VARCHAR(20) NULL,
    EmailNguoiBao VARCHAR(100) NULL,

    MaNguoiTiepNhan INT NULL,
    MaDonVi INT NULL,

    MucDoUuTien NVARCHAR(20) NOT NULL DEFAULT N'BinhThuong',
    TrangThai NVARCHAR(30) NOT NULL DEFAULT N'MoiTiepNhan',
    GhiChuXuLy NVARCHAR(1000) NULL,

    NgayTiepNhan DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    NgayCapNhat DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT FK_TinBao_LoaiTinBao
        FOREIGN KEY (MaLoaiTinBao)
        REFERENCES LoaiTinBao(MaLoaiTinBao),

    CONSTRAINT FK_TinBao_NguoiTiepNhan
        FOREIGN KEY (MaNguoiTiepNhan)
        REFERENCES NguoiDung(MaNguoiDung),

    CONSTRAINT FK_TinBao_DonVi
        FOREIGN KEY (MaDonVi)
        REFERENCES DonVi(MaDonVi),

    CONSTRAINT CK_TinBao_NguonTin
        CHECK (NguonTin IN (N'NguoiDan', N'CanBo')),

    CONSTRAINT CK_TinBao_MucDoUuTien
        CHECK (MucDoUuTien IN (N'Thap', N'BinhThuong', N'Cao', N'KhanCap')),

    CONSTRAINT CK_TinBao_TrangThai
        CHECK (TrangThai IN (N'MoiTiepNhan', N'DangXuLy', N'DaChuyenXuLy', N'DaHoanThanh', N'DaHuy'))
);
GO

CREATE INDEX IX_TinBao_TrangThai ON TinBao(TrangThai);
CREATE INDEX IX_TinBao_ThoiGianXayRa ON TinBao(ThoiGianXayRa);
CREATE INDEX IX_TinBao_LoaiTinBao ON TinBao(MaLoaiTinBao);
GO

CREATE TABLE TepTinBao
(
    MaTep INT IDENTITY(1,1) PRIMARY KEY,
    MaTinBao INT NOT NULL,

    TenTep NVARCHAR(255) NOT NULL,
    DuongDan NVARCHAR(500) NOT NULL,
    LoaiTep NVARCHAR(20) NOT NULL,
    KichThuoc BIGINT NULL,

    MaNguoiTaiLen INT NULL,
    ThoiGianTaiLen DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT FK_TepTinBao_TinBao
        FOREIGN KEY (MaTinBao)
        REFERENCES TinBao(MaTinBao)
        ON DELETE CASCADE,

    CONSTRAINT FK_TepTinBao_NguoiDung
        FOREIGN KEY (MaNguoiTaiLen)
        REFERENCES NguoiDung(MaNguoiDung),

    CONSTRAINT CK_TepTinBao_LoaiTep
        CHECK (LoaiTep IN (N'Anh', N'Video', N'TaiLieu'))
);
GO

CREATE INDEX IX_TepTinBao_TinBao ON TepTinBao(MaTinBao);
GO


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