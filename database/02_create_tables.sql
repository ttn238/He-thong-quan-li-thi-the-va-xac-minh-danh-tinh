USE QL_XACMINHTHITHE;
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
 
CREATE TABLE VaiTro
(
    MaVaiTro INT IDENTITY(1,1) PRIMARY KEY,
    MaCode VARCHAR(50) NOT NULL UNIQUE,
    TenVaiTro NVARCHAR(100) NOT NULL,
    MoTa NVARCHAR(255) NULL,
    TrangThai BIT NOT NULL DEFAULT 1,
    NgayTao DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

CREATE TABLE QuyenHeThong
(
    MaQuyen INT IDENTITY(1,1) PRIMARY KEY,
    Resource VARCHAR(80) NOT NULL,
    [Action] VARCHAR(30) NOT NULL,

    CONSTRAINT UQ_QuyenHeThong_Resource_Action
        UNIQUE (Resource, [Action])
);
GO
 
CREATE TABLE VaiTro_Quyen
(
    MaVaiTro INT NOT NULL,
    MaQuyen INT NOT NULL,
    PhamVi VARCHAR(10) NOT NULL DEFAULT 'ALL',   -- thuoc tinh "Pham vi du lieu" cua quan he

    CONSTRAINT PK_VaiTro_Quyen
        PRIMARY KEY (MaVaiTro, MaQuyen),

    CONSTRAINT FK_VaiTroQuyen_VaiTro
        FOREIGN KEY (MaVaiTro) REFERENCES VaiTro(MaVaiTro),

    CONSTRAINT FK_VaiTroQuyen_Quyen
        FOREIGN KEY (MaQuyen) REFERENCES QuyenHeThong(MaQuyen),

    CONSTRAINT CK_VaiTroQuyen_PhamVi
        CHECK (PhamVi IN ('ALL', 'OWN', 'PARTIAL'))
);
GO

CREATE TABLE ThamSoHeThong
(
    MaThamSo INT IDENTITY(1,1) PRIMARY KEY,
    GiaTri NVARCHAR(MAX) NOT NULL,
    MoTa NVARCHAR(500)
);
GO
 
CREATE TABLE TaiKhoanNguoiDung
(
    MaTaiKhoan BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenDangNhap VARCHAR(50) NOT NULL UNIQUE,
    Email VARCHAR(150),
    SoDienThoai VARCHAR(15),
    CCCD VARCHAR(20) NULL,
    MatKhauHash VARCHAR(255) NOT NULL,
    HoTen NVARCHAR(150) NOT NULL,
    DonVi NVARCHAR(150),
    ChucVu NVARCHAR(100) NULL,
    TrangThai NVARCHAR(20) NOT NULL DEFAULT N'HoatDong',
    MaVaiTro INT NOT NULL,
    SoLanDangNhapSai INT NOT NULL DEFAULT 0,
    KhoaDenThoiDiem DATETIME2 NULL,
    LanDangNhapCuoi DATETIME2 NULL,
    NgayTao DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    NgayCapNhat DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT FK_TaiKhoan_VaiTro
        FOREIGN KEY (MaVaiTro) REFERENCES VaiTro(MaVaiTro),

    CONSTRAINT CK_TaiKhoan_TrangThai
        CHECK (TrangThai IN (N'HoatDong', N'KhoaTam', N'Khoa')),

    CONSTRAINT CK_TaiKhoan_LienHe
        CHECK (Email IS NOT NULL OR SoDienThoai IS NOT NULL)
);
GO

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

    MaTaiKhoanTiepNhan BIGINT NULL,

    MucDoUuTien NVARCHAR(20) NOT NULL DEFAULT N'BinhThuong',
    TrangThai NVARCHAR(30) NOT NULL DEFAULT N'MoiTiepNhan',
    GhiChuXuLy NVARCHAR(1000) NULL,

    NgayTiepNhan DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    NgayCapNhat DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT FK_TinBao_LoaiTinBao
        FOREIGN KEY (MaLoaiTinBao)
        REFERENCES LoaiTinBao(MaLoaiTinBao),

    CONSTRAINT FK_TinBao_TaiKhoanTiepNhan
        FOREIGN KEY (MaTaiKhoanTiepNhan)
        REFERENCES TaiKhoanNguoiDung(MaTaiKhoan),

    CONSTRAINT CK_TinBao_NguonTin
        CHECK (NguonTin IN (N'NguoiDan', N'CanBo')),

    CONSTRAINT CK_TinBao_MucDoUuTien
        CHECK (MucDoUuTien IN (N'Thap', N'BinhThuong', N'Cao', N'KhanCap')),

    CONSTRAINT CK_TinBao_TrangThai
        CHECK (TrangThai IN (N'MoiTiepNhan', N'DaTiepNhan', N'YeuCauBoSung', N'TuChoi', N'DangXacMinh', N'DaXuLy'))
);
GO
 
 CREATE TABLE TepTinBao
(
    MaTep INT IDENTITY(1,1) PRIMARY KEY,
    MaTinBao INT NOT NULL,

    TenTep NVARCHAR(255) NOT NULL,
    DuongDan NVARCHAR(500) NOT NULL,
    LoaiTep NVARCHAR(20) NOT NULL,
    KichThuoc BIGINT NULL,

    MaNguoiTaiLen BIGINT NULL,
    ThoiGianTaiLen DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT FK_TepTinBao_TinBao
        FOREIGN KEY (MaTinBao)
        REFERENCES TinBao(MaTinBao)
        ON DELETE CASCADE,

    CONSTRAINT FK_TepTinBao_TaiKhoan
        FOREIGN KEY (MaNguoiTaiLen)
        REFERENCES TaiKhoanNguoiDung(MaTaiKhoan),

    CONSTRAINT CK_TepTinBao_LoaiTep
        CHECK (LoaiTep IN (N'Anh', N'Video', N'TaiLieu'))
);
GO

CREATE TABLE HoSoThiThe
(
    MaHoSoThiThe BIGINT IDENTITY(1,1) PRIMARY KEY,
    ThoiGianPhatHien DATETIME2 NOT NULL,
    DiaDiem NVARCHAR(255),
    TrangThai NVARCHAR(30) NOT NULL DEFAULT N'MoiTiepNhan',
    GioiTinh NVARCHAR(10),
    TuoiUocLuong SMALLINT,
    ChieuCao DECIMAL(5,2),                -- don vi: cm
    DacDiemCoThe NVARCHAR(MAX),
    KhuonMat VARBINARY(MAX),              -- vector dac trung khuon mat
    MaTinBao INT NULL,

    CONSTRAINT FK_HoSoThiThe_TinBao
        FOREIGN KEY (MaTinBao) REFERENCES TinBao(MaTinBao),

    CONSTRAINT CK_HoSoThiThe_GioiTinh
        CHECK (GioiTinh IS NULL OR GioiTinh IN (N'Nam', N'Nu', N'Khac', N'KhongXacDinh')),

    CONSTRAINT CK_HoSoThiThe_Tuoi
        CHECK (TuoiUocLuong IS NULL OR TuoiUocLuong BETWEEN 0 AND 130),

    CONSTRAINT CK_HoSoThiThe_ChieuCao
        CHECK (ChieuCao IS NULL OR ChieuCao > 0)
);
GO
 
CREATE TABLE HoSoNguoiMatTich
(
    MaHoSoMatTich BIGINT IDENTITY(1,1) PRIMARY KEY,
    HoTen NVARCHAR(150) NOT NULL,
    NgaySinh DATE,
    GioiTinh NVARCHAR(10),
    ThoiGianMatTich DATETIME2,
    NoiMatTich NVARCHAR(255),
    TrangThai NVARCHAR(30) NOT NULL DEFAULT N'DangTimKiem',

    CONSTRAINT CK_HoSoMatTich_GioiTinh
        CHECK (GioiTinh IS NULL OR GioiTinh IN (N'Nam', N'Nu', N'Khac'))
);
GO

CREATE TABLE NguoiThan
(
    MaNguoiThan BIGINT IDENTITY(1,1) PRIMARY KEY,
    HoTen NVARCHAR(150) NOT NULL,
    QuanHe NVARCHAR(50),
    SoDienThoai VARCHAR(15),
    DiaChi NVARCHAR(255),
    MaHoSoMatTich BIGINT NOT NULL,

    CONSTRAINT FK_NguoiThan_HoSoMatTich
        FOREIGN KEY (MaHoSoMatTich) REFERENCES HoSoNguoiMatTich(MaHoSoMatTich)
);
GO
 
CREATE TABLE TepTin
(
    MaTep BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenFile NVARCHAR(255) NOT NULL,
    DuongDan NVARCHAR(500) NOT NULL,
    LoaiFile VARCHAR(50),
    KichThuoc BIGINT,                     -- don vi: byte
    ThoiGianTaiLen DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    MaHoSoThiThe BIGINT NULL,
    MaHoSoMatTich BIGINT NULL,

    CONSTRAINT FK_TepTin_HoSoThiThe
        FOREIGN KEY (MaHoSoThiThe) REFERENCES HoSoThiThe(MaHoSoThiThe),

    CONSTRAINT FK_TepTin_HoSoMatTich
        FOREIGN KEY (MaHoSoMatTich) REFERENCES HoSoNguoiMatTich(MaHoSoMatTich),

    CONSTRAINT CK_TepTin_KichThuoc
        CHECK (KichThuoc IS NULL OR KichThuoc >= 0),

    -- Mot tep chi thuoc toi da 1 ho so (thi the HOAC mat tich)
    CONSTRAINT CK_TepTin_MotHoSo
        CHECK (MaHoSoThiThe IS NULL OR MaHoSoMatTich IS NULL)
);
GO

CREATE TABLE KetQuaOCR
(
    MaKetQua BIGINT IDENTITY(1,1) PRIMARY KEY,
    NoiDung NVARCHAR(MAX),
    DoTinCay DECIMAL(5,2),                -- thang 0-100
    LoaiDuLieu NVARCHAR(50),
    ThoiGianXuLy DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    MaTep BIGINT NOT NULL,

    CONSTRAINT FK_KetQuaOCR_TepTin
        FOREIGN KEY (MaTep) REFERENCES TepTin(MaTep),

    CONSTRAINT CK_KetQuaOCR_DoTinCay
        CHECK (DoTinCay IS NULL OR DoTinCay BETWEEN 0 AND 100)
);
GO

CREATE TABLE PhuongTien
(
    MaPhuongTien BIGINT IDENTITY(1,1) PRIMARY KEY,
    BienSo VARCHAR(20),
    LoaiXe NVARCHAR(50),
    NhanHieu NVARCHAR(50),
    MauSac NVARCHAR(30),
    ChuXe NVARCHAR(150),
    MaKetQua BIGINT NOT NULL,

    CONSTRAINT FK_PhuongTien_KetQuaOCR
        FOREIGN KEY (MaKetQua) REFERENCES KetQuaOCR(MaKetQua)
);
GO
 
CREATE TABLE LoaiDoVat
(
    MaLoaiDoVat INT IDENTITY(1,1) PRIMARY KEY,
    TenLoai NVARCHAR(100) NOT NULL UNIQUE
);
GO

CREATE TABLE DoVatCaNhan
(
    MaDoVat BIGINT IDENTITY(1,1) PRIMARY KEY,
    Ten NVARCHAR(150) NOT NULL,
    MoTa NVARCHAR(500),
    TinhTrang NVARCHAR(100),
    MaHoSoThiThe BIGINT NOT NULL,
    MaLoaiDoVat INT NOT NULL,

    CONSTRAINT FK_DoVat_HoSoThiThe
        FOREIGN KEY (MaHoSoThiThe) REFERENCES HoSoThiThe(MaHoSoThiThe),

    CONSTRAINT FK_DoVat_LoaiDoVat
        FOREIGN KEY (MaLoaiDoVat) REFERENCES LoaiDoVat(MaLoaiDoVat)
);
GO
 
CREATE TABLE ViTriLuuTru
(
    MaViTri INT IDENTITY(1,1) PRIMARY KEY,
    Khu NVARCHAR(50) NOT NULL,
    Tu NVARCHAR(50) NOT NULL,
    Ngan NVARCHAR(50) NOT NULL,
    SucChua INT NOT NULL DEFAULT 1,

    CONSTRAINT UQ_ViTriLuuTru_Khu_Tu_Ngan
        UNIQUE (Khu, Tu, Ngan),

    CONSTRAINT CK_ViTriLuuTru_SucChua
        CHECK (SucChua > 0)
);
GO

CREATE TABLE LichSuLuuTru
(
    MaLichSu BIGINT IDENTITY(1,1) PRIMARY KEY,
    ThoiGian DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    MaHoSoThiThe BIGINT NOT NULL,
    MaViTri INT NOT NULL,

    CONSTRAINT FK_LichSuLuuTru_HoSoThiThe
        FOREIGN KEY (MaHoSoThiThe) REFERENCES HoSoThiThe(MaHoSoThiThe),

    CONSTRAINT FK_LichSuLuuTru_ViTri
        FOREIGN KEY (MaViTri) REFERENCES ViTriLuuTru(MaViTri)
);
GO
 
CREATE TABLE DoiSanh
(
    MaDoiSanh BIGINT IDENTITY(1,1) PRIMARY KEY,
    TieuChi NVARCHAR(255),
    ThoiGianYeuCau DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    DiemDoiSanh DECIMAL(5,2),             -- thang 0-100
    MucDoPhuHop NVARCHAR(30),
    TrangThai NVARCHAR(30) NOT NULL DEFAULT N'ChoXuLy',
    MaHoSoThiThe BIGINT NOT NULL,
    MaHoSoMatTich BIGINT NOT NULL,
    MaTaiKhoanYeuCau BIGINT NOT NULL,

    CONSTRAINT FK_DoiSanh_HoSoThiThe
        FOREIGN KEY (MaHoSoThiThe) REFERENCES HoSoThiThe(MaHoSoThiThe),

    CONSTRAINT FK_DoiSanh_HoSoMatTich
        FOREIGN KEY (MaHoSoMatTich) REFERENCES HoSoNguoiMatTich(MaHoSoMatTich),

    CONSTRAINT FK_DoiSanh_TaiKhoanYeuCau
        FOREIGN KEY (MaTaiKhoanYeuCau) REFERENCES TaiKhoanNguoiDung(MaTaiKhoan),

    CONSTRAINT CK_DoiSanh_Diem
        CHECK (DiemDoiSanh IS NULL OR DiemDoiSanh BETWEEN 0 AND 100)
);
GO
 
CREATE TABLE XacMinh
(
    MaXacMinh BIGINT IDENTITY(1,1) PRIMARY KEY,
    PhuongPhap NVARCHAR(100),
    ThoiGian DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    TrangThai NVARCHAR(30) NOT NULL DEFAULT N'DangXacMinh',
    KetQua NVARCHAR(50),
    NhanXet NVARCHAR(1000),
    MucDoChacChan DECIMAL(5,2),           -- thang 0-100
    BangChung NVARCHAR(MAX),
    MaDoiSanh BIGINT NOT NULL,
    MaTaiKhoanThucHien BIGINT NOT NULL,

    CONSTRAINT FK_XacMinh_DoiSanh
        FOREIGN KEY (MaDoiSanh) REFERENCES DoiSanh(MaDoiSanh),

    CONSTRAINT FK_XacMinh_TaiKhoanThucHien
        FOREIGN KEY (MaTaiKhoanThucHien) REFERENCES TaiKhoanNguoiDung(MaTaiKhoan),

    CONSTRAINT CK_XacMinh_MucDoChacChan
        CHECK (MucDoChacChan IS NULL OR MucDoChacChan BETWEEN 0 AND 100)
);
GO

CREATE TABLE PheDuyet
(
    MaPheDuyet BIGINT IDENTITY(1,1) PRIMARY KEY,
    ThoiGian DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    TrangThai NVARCHAR(30) NOT NULL DEFAULT N'ChoDuyet',
    YKien NVARCHAR(1000),
    MaXacMinh BIGINT NOT NULL UNIQUE,     -- UNIQUE de dam bao quan he 1-1
    MaNguoiDuyet BIGINT NOT NULL,         -- thuoc tinh "Nguoi duyet" -> tro den tai khoan

    CONSTRAINT FK_PheDuyet_XacMinh
        FOREIGN KEY (MaXacMinh) REFERENCES XacMinh(MaXacMinh),

    CONSTRAINT FK_PheDuyet_NguoiDuyet
        FOREIGN KEY (MaNguoiDuyet) REFERENCES TaiKhoanNguoiDung(MaTaiKhoan)
);
GO

CREATE TABLE BanGiao
(
    MaBanGiao BIGINT IDENTITY(1,1) PRIMARY KEY,
    ThoiGian DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    DiaDiem NVARCHAR(255),
    TrangThai NVARCHAR(30) NOT NULL DEFAULT N'ChoBanGiao',
    MaXacMinh BIGINT NOT NULL,
    MaNguoiThan BIGINT NOT NULL,
    MaNguoiGiao BIGINT NOT NULL,          -- thuoc tinh "Nguoi giao" -> tro den tai khoan

    CONSTRAINT FK_BanGiao_XacMinh
        FOREIGN KEY (MaXacMinh) REFERENCES XacMinh(MaXacMinh),

    CONSTRAINT FK_BanGiao_NguoiThan
        FOREIGN KEY (MaNguoiThan) REFERENCES NguoiThan(MaNguoiThan),

    CONSTRAINT FK_BanGiao_NguoiGiao
        FOREIGN KEY (MaNguoiGiao) REFERENCES TaiKhoanNguoiDung(MaTaiKhoan)
);
GO

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

    MaHoSoThiThe BIGINT NULL,
    MaHoSoMatTich BIGINT NULL,
    MaTinBao INT NULL,
    MaXacMinh BIGINT NULL,

    MaTaiKhoanTaiLen BIGINT NOT NULL,
    ThoiGianTaiLen DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    TrangThai NVARCHAR(20) NOT NULL DEFAULT N'HoatDong',

    CONSTRAINT FK_HinhAnh_HoSoThiThe
        FOREIGN KEY (MaHoSoThiThe) REFERENCES HoSoThiThe(MaHoSoThiThe),

    CONSTRAINT FK_HinhAnh_HoSoMatTich
        FOREIGN KEY (MaHoSoMatTich) REFERENCES HoSoNguoiMatTich(MaHoSoMatTich),

    CONSTRAINT FK_HinhAnh_TinBao
        FOREIGN KEY (MaTinBao) REFERENCES TinBao(MaTinBao),

    CONSTRAINT FK_HinhAnh_XacMinh
        FOREIGN KEY (MaXacMinh) REFERENCES XacMinh(MaXacMinh),

    CONSTRAINT FK_HinhAnh_TaiKhoan
        FOREIGN KEY (MaTaiKhoanTaiLen) REFERENCES TaiKhoanNguoiDung(MaTaiKhoan)
);
GO
 
CREATE TABLE NhatKyHeThong
(
    MaNhatKy BIGINT IDENTITY(1,1) PRIMARY KEY,
    HanhDong NVARCHAR(100) NOT NULL,
    ThoiGian DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    DoiTuongTacDong NVARCHAR(255),
    MaTaiKhoan BIGINT NULL,

    CONSTRAINT FK_NhatKy_TaiKhoan
        FOREIGN KEY (MaTaiKhoan) REFERENCES TaiKhoanNguoiDung(MaTaiKhoan)
);
GO

CREATE TABLE ThongBao
(
    MaThongBao BIGINT IDENTITY(1,1) PRIMARY KEY,
    NoiDung NVARCHAR(1000) NOT NULL,
    ThoiGian DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    TrangThaiDoc BIT NOT NULL DEFAULT 0,
    MaTaiKhoan BIGINT NOT NULL,

    CONSTRAINT FK_ThongBao_TaiKhoan
        FOREIGN KEY (MaTaiKhoan) REFERENCES TaiKhoanNguoiDung(MaTaiKhoan)
);
GO
 
