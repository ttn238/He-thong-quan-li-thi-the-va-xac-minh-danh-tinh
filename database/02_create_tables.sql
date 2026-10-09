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

-- Ho so nguoi mat tich do nguoi than khai bao
CREATE TABLE HoSoNguoiMatTich (
    MaHoSoMatTich    INT IDENTITY(1,1) PRIMARY KEY,
    MaNguoiDung      INT           NOT NULL,
    HoTen            NVARCHAR(100) NOT NULL,
    GioiTinh         NVARCHAR(5)   NULL,
    Tuoi             INT           NULL,
    ChieuCao         INT           NULL,                  -- cm
    DacDiemNhanDang  NVARCHAR(300) NULL,
    Seo              NVARCHAR(200) NULL,
    HinhXam          NVARCHAR(200) NULL,
    TrangPhuc        NVARCHAR(200) NULL,
    ThoiGianMatTich  DATETIME2     NULL,
    DiaDiemCuoiThay  NVARCHAR(200) NULL,
    QuanHeVoiNguoiKhai NVARCHAR(50) NULL,
    SoLienHe         VARCHAR(15)   NULL,
    NgayKhai         DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    TrangThai        NVARCHAR(15)  NOT NULL DEFAULT N'DangTimKiem'                    
);

-- Ho so thi the (chua xac dinh / da xac dinh danh tinh)
CREATE TABLE HoSoThiThe (
    MaHoSoThiThe     INT IDENTITY(1,1) PRIMARY KEY,
    MaHoSo           AS ('HS' + RIGHT('000' + CAST(MaHoSoThiThe AS VARCHAR(10)), 3)) PERSISTED,
    MaTinBao         INT           NULL REFERENCES TinBao(MaTinBao),
    ThoiGianPhatHien DATETIME2     NOT NULL,
    DiaDiem          NVARCHAR(200) NULL,
    GioiTinh         NVARCHAR(5)   NULL,
    TuoiUocTinh      INT           NULL,
    ChieuCao         INT           NULL,                  -- cm
    DacDiemNhanDang  NVARCHAR(300) NULL,
    Seo              NVARCHAR(200) NULL,
    HinhXam          NVARCHAR(200) NULL,
    TrangPhuc        NVARCHAR(200) NULL,
    TinhTrang        NVARCHAR(200) NULL,
    MaViTri          INT           NULL REFERENCES ViTriLuuTru(MaViTri),
    TrangThai        NVARCHAR(15)  NOT NULL DEFAULT N'MoiTiepNhan',
    MaCanBoLap       INT           NOT NULL REFERENCES CanBo(MaCanBo),
    NgayLap          DATETIME2     NOT NULL DEFAULT SYSDATETIME()
);

CREATE TABLE ThongBao (
    MaThongBao INT IDENTITY(1,1) PRIMARY KEY,
    MaTaiKhoan INT           NOT NULL REFERENCES TaiKhoan(MaTaiKhoan),
    TieuDe     NVARCHAR(150) NOT NULL,
    NoiDung    NVARCHAR(500) NULL,
    DaDoc      BIT           NOT NULL DEFAULT 0,
    NgayTao    DATETIME2     NOT NULL DEFAULT SYSDATETIME()
);

CREATE TABLE NhatKyHeThong (
    MaNhatKy   BIGINT IDENTITY(1,1) PRIMARY KEY,
    MaTaiKhoan INT           NULL REFERENCES TaiKhoan(MaTaiKhoan),
    HanhDong   NVARCHAR(50)  NOT NULL,     -- DangNhap, Them, Sua, Xoa, XacNhan, BanGiao...
    BangTacDong NVARCHAR(50) NULL,
    MaDoiTuong INT           NULL,
    ChiTiet    NVARCHAR(500) NULL,
    ThoiGian   DATETIME2     NOT NULL DEFAULT SYSDATETIME()
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
