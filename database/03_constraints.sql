/* FOREIGN KEY HO SO */
ALTER TABLE HoSoNguoiMatTich
ADD CONSTRAINT FK_HoSoNguoiMatTich_NguoiDung
FOREIGN KEY (MaNguoiDung)
REFERENCES NguoiDung(MaNguoiDung);
GO

ALTER TABLE HoSoThiThe
ADD CONSTRAINT FK_HoSoThiThe_TinBao
FOREIGN KEY (MaTinBao)
REFERENCES TinBao(MaTinBao);
GO

ALTER TABLE HoSoThiThe
ADD CONSTRAINT FK_HoSoThiThe_ViTriLuuTru
FOREIGN KEY (MaViTri)
REFERENCES ViTriLuuTru(MaViTri);
GO

ALTER TABLE HoSoThiThe
ADD CONSTRAINT FK_HoSoThiThe_CanBo
FOREIGN KEY (MaCanBoLap)
REFERENCES CanBo(MaCanBo);
GO

-- FOREIGN KEY THONG BAO & NHAT KY --
ALTER TABLE ThongBao
ADD CONSTRAINT FK_ThongBao_TaiKhoan
FOREIGN KEY (MaTaiKhoan)
REFERENCES TaiKhoan(MaTaiKhoan);
GO

ALTER TABLE NhatKyHeThong
ADD CONSTRAINT FK_NhatKyHeThong_TaiKhoan
FOREIGN KEY (MaTaiKhoan)
REFERENCES TaiKhoan(MaTaiKhoan);
GO

-- CHECK CONSTRAINT --
ALTER TABLE HoSoNguoiMatTich
ADD CONSTRAINT CK_HoSoNguoiMatTich_GioiTinh
CHECK (GioiTinh IN (N'Nam', N'Nữ'));
GO

ALTER TABLE HoSoNguoiMatTich
ADD CONSTRAINT CK_HoSoNguoiMatTich_Tuoi
CHECK (Tuoi BETWEEN 0 AND 120);
GO

ALTER TABLE HoSoNguoiMatTich
ADD CONSTRAINT CK_HoSoNguoiMatTich_TrangThai
CHECK (TrangThai IN (
    N'DangTimKiem',
    N'DaTimThay',
    N'DaDong'
));
GO

ALTER TABLE HoSoThiThe
ADD CONSTRAINT CK_HoSoThiThe_GioiTinh
CHECK (GioiTinh IN (N'Nam', N'Nữ'));
GO

ALTER TABLE HoSoThiThe
ADD CONSTRAINT CK_HoSoThiThe_TrangThai
CHECK (TrangThai IN (
    N'MoiTiepNhan',
    N'DangLuuGiu',
    N'DangXacMinh',
    N'ChuaXacDinh',
    N'DaXacDinh',
    N'DaBanGiao',
    N'DongHoSo'
));
GO