USE QL_XACMINHTHITHE;
GO
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
  
/* 1. Them cot TenThamSo (ERD chi co Ma/GiaTri/MoTa nen khong tra cuu duoc tham so) */
ALTER TABLE ThamSoHeThong ADD TenThamSo VARCHAR(100) NOT NULL;
GO
ALTER TABLE ThamSoHeThong ADD CONSTRAINT UQ_ThamSo_TenThamSo UNIQUE (TenThamSo);
GO

/* 2. Quyen he thong */
ALTER TABLE QuyenHeThong ADD CONSTRAINT CK_Quyen_Action
    CHECK ([Action] IN ('VIEW','CREATE','UPDATE','DELETE','CONFIRM','HANDOVER','APPROVE','EXPORT'));
GO

/* 3. Tai khoan: dinh dang email, so dien thoai */
ALTER TABLE TaiKhoanNguoiDung ADD
    CONSTRAINT CK_TaiKhoan_Email
        CHECK (Email IS NULL OR (Email LIKE '%_@_%._%' AND Email NOT LIKE '% %')),
    CONSTRAINT CK_TaiKhoan_SDT
        CHECK (SoDienThoai IS NULL
               OR (SoDienThoai NOT LIKE '%[^0-9+]%' AND LEN(SoDienThoai) BETWEEN 9 AND 12));
GO

/* 4. Tin bao */
ALTER TABLE TinBao ADD CONSTRAINT CK_TinBao_TrangThai
    CHECK (TrangThai IN (N'MoiTiepNhan', N'DaTiepNhan', N'YeuCauBoSung', N'TuChoi', N'DangXacMinh', N'DaXuLy'));
GO

/* 5. Ho so thi the */
ALTER TABLE HoSoThiThe ADD CONSTRAINT CK_HoSoThiThe_TrangThai
    CHECK (TrangThai IN (N'MoiTiepNhan', N'DangLuuGiu', N'DangXacMinh', N'TiepTucLuuGiu',
                         N'DaXacDinh', N'DaBanGiao', N'DongHoSo'));
GO

/* 6. Ho so nguoi mat tich, nguoi than */
ALTER TABLE HoSoNguoiMatTich ADD
    CONSTRAINT CK_HoSoMatTich_TrangThai
        CHECK (TrangThai IN (N'DangTimKiem', N'CoHoSoNghiVan', N'DaTimThay', N'DaDong')),
    CONSTRAINT CK_HoSoMatTich_NgaySinh
        CHECK (NgaySinh IS NULL OR NgaySinh <= CAST(SYSDATETIME() AS DATE));
GO

ALTER TABLE NguoiThan ADD CONSTRAINT CK_NguoiThan_SDT
    CHECK (SoDienThoai IS NULL
           OR (SoDienThoai NOT LIKE '%[^0-9+]%' AND LEN(SoDienThoai) BETWEEN 9 AND 12));
GO

/* 7. Doi sanh */
ALTER TABLE DoiSanh ADD
    CONSTRAINT CK_DoiSanh_TrangThai
        CHECK (TrangThai IN (N'ChoXuLy', N'ChuyenXacMinh', N'LoaiBo')),
    CONSTRAINT CK_DoiSanh_MucDo
        CHECK (MucDoPhuHop IS NULL OR MucDoPhuHop IN (N'Thap', N'CoKhaNang', N'CanXacMinh', N'UuTien')),
    CONSTRAINT UQ_DoiSanh_Cap UNIQUE (MaHoSoThiThe, MaHoSoMatTich);   -- moi cap chi 1 dong doi sanh
GO

/* 8. Xac minh
   Luu y: dung ISNULL vi CHECK cho qua khi ket qua la UNKNOWN (NULL) */
ALTER TABLE XacMinh ADD
    CONSTRAINT CK_XacMinh_TrangThai
        CHECK (TrangThai IN (N'DangXacMinh', N'ChoDuyet', N'DaDuyet', N'TuChoiDuyet', N'KhongPhuHop')),
    CONSTRAINT CK_XacMinh_KetQua
        CHECK (KetQua IS NULL OR KetQua IN (N'Khop', N'KhongKhop')),
    CONSTRAINT CK_XacMinh_TrinhDuyet
        CHECK (TrangThai NOT IN (N'ChoDuyet', N'DaDuyet', N'TuChoiDuyet') OR ISNULL(KetQua, N'') = N'Khop'),
    CONSTRAINT CK_XacMinh_KhongPhuHop
        CHECK (TrangThai <> N'KhongPhuHop' OR (ISNULL(KetQua, N'') = N'KhongKhop' AND NhanXet IS NOT NULL));
GO

/* 9. Phe duyet */
ALTER TABLE PheDuyet ADD
    CONSTRAINT CK_PheDuyet_TrangThai
        CHECK (TrangThai IN (N'ChoDuyet', N'Duyet', N'TuChoi')),
    CONSTRAINT CK_PheDuyet_LyDoTuChoi
        CHECK (TrangThai <> N'TuChoi' OR YKien IS NOT NULL);
GO

/* 10. Ban giao */
ALTER TABLE BanGiao ADD CONSTRAINT CK_BanGiao_TrangThai
    CHECK (TrangThai IN (N'ChoBanGiao', N'DangKiemTra', N'DaGiao', N'DaXacNhan', N'Huy'));
GO

/* 11. UNIQUE co dieu kien (luat nghiep vu) */
CREATE UNIQUE INDEX UQ_TaiKhoan_Email ON TaiKhoanNguoiDung(Email)       WHERE Email IS NOT NULL;
CREATE UNIQUE INDEX UQ_TaiKhoan_SDT   ON TaiKhoanNguoiDung(SoDienThoai) WHERE SoDienThoai IS NOT NULL;
-- Moi ket luan xac minh chi co 1 phieu ban giao con hieu luc
CREATE UNIQUE INDEX UQ_BanGiao_HieuLuc ON BanGiao(MaXacMinh) WHERE TrangThai <> N'Huy';
GO