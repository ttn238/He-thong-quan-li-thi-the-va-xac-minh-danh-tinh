USE QL_XACMINHTHITHE;
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TaiKhoan_MaVaiTro')
    CREATE INDEX IX_TaiKhoan_MaVaiTro        ON TaiKhoanNguoiDung(MaVaiTro);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_VaiTroQuyen_MaQuyen')
    CREATE INDEX IX_VaiTroQuyen_MaQuyen      ON VaiTro_Quyen(MaQuyen);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TinBao_MaLoaiTinBao')
    CREATE INDEX IX_TinBao_MaLoaiTinBao      ON TinBao(MaLoaiTinBao);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TinBao_TiepNhan')
    CREATE INDEX IX_TinBao_TiepNhan          ON TinBao(MaTaiKhoanTiepNhan);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TepTinBao_TinBao')
    CREATE INDEX IX_TepTinBao_TinBao         ON TepTinBao(MaTinBao);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_HoSoThiThe_MaTinBao')
    CREATE INDEX IX_HoSoThiThe_MaTinBao      ON HoSoThiThe(MaTinBao);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_NguoiThan_HoSoMatTich')
    CREATE INDEX IX_NguoiThan_HoSoMatTich    ON NguoiThan(MaHoSoMatTich);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TepTin_HoSoThiThe')
    CREATE INDEX IX_TepTin_HoSoThiThe        ON TepTin(MaHoSoThiThe);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TepTin_HoSoMatTich')
    CREATE INDEX IX_TepTin_HoSoMatTich       ON TepTin(MaHoSoMatTich);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_KetQuaOCR_MaTep')
    CREATE INDEX IX_KetQuaOCR_MaTep          ON KetQuaOCR(MaTep);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PhuongTien_MaKetQua')
    CREATE INDEX IX_PhuongTien_MaKetQua      ON PhuongTien(MaKetQua);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PhuongTien_BienSo')
    CREATE INDEX IX_PhuongTien_BienSo        ON PhuongTien(BienSo);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DoVat_HoSoThiThe')
    CREATE INDEX IX_DoVat_HoSoThiThe         ON DoVatCaNhan(MaHoSoThiThe);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DoVat_LoaiDoVat')
    CREATE INDEX IX_DoVat_LoaiDoVat          ON DoVatCaNhan(MaLoaiDoVat);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LichSuLuuTru_HoSo')
    CREATE INDEX IX_LichSuLuuTru_HoSo        ON LichSuLuuTru(MaHoSoThiThe, ThoiGian DESC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LichSuLuuTru_ViTri')
    CREATE INDEX IX_LichSuLuuTru_ViTri       ON LichSuLuuTru(MaViTri);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DoiSanh_HoSoThiThe')
    CREATE INDEX IX_DoiSanh_HoSoThiThe       ON DoiSanh(MaHoSoThiThe);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DoiSanh_HoSoMatTich')
    CREATE INDEX IX_DoiSanh_HoSoMatTich      ON DoiSanh(MaHoSoMatTich);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DoiSanh_TaiKhoanYeuCau')
    CREATE INDEX IX_DoiSanh_TaiKhoanYeuCau   ON DoiSanh(MaTaiKhoanYeuCau);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_XacMinh_DoiSanh')
    CREATE INDEX IX_XacMinh_DoiSanh          ON XacMinh(MaDoiSanh);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_XacMinh_TaiKhoan')
    CREATE INDEX IX_XacMinh_TaiKhoan         ON XacMinh(MaTaiKhoanThucHien);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PheDuyet_NguoiDuyet')
    CREATE INDEX IX_PheDuyet_NguoiDuyet      ON PheDuyet(MaNguoiDuyet);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BanGiao_XacMinh')
    CREATE INDEX IX_BanGiao_XacMinh          ON BanGiao(MaXacMinh);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BanGiao_NguoiThan')
    CREATE INDEX IX_BanGiao_NguoiThan        ON BanGiao(MaNguoiThan);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BanGiao_NguoiGiao')
    CREATE INDEX IX_BanGiao_NguoiGiao        ON BanGiao(MaNguoiGiao);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_NhatKy_TaiKhoan_ThoiGian')
    CREATE INDEX IX_NhatKy_TaiKhoan_ThoiGian ON NhatKyHeThong(MaTaiKhoan, ThoiGian DESC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ThongBao_TaiKhoan')
    CREATE INDEX IX_ThongBao_TaiKhoan        ON ThongBao(MaTaiKhoan, TrangThaiDoc);

-- Chi muc cho LichSuDangNhap & HinhAnh
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LichSuDangNhap_ThoiGian')
    CREATE INDEX IX_LichSuDangNhap_ThoiGian  ON LichSuDangNhap(ThoiGian DESC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LichSuDangNhap_TaiKhoan')
    CREATE INDEX IX_LichSuDangNhap_TaiKhoan  ON LichSuDangNhap(MaTaiKhoan);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_HinhAnh_HoSoThiThe')
    CREATE INDEX IX_HinhAnh_HoSoThiThe       ON HinhAnh(MaHoSoThiThe) WHERE TrangThai = N'HoatDong';
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_HinhAnh_HoSoMatTich')
    CREATE INDEX IX_HinhAnh_HoSoMatTich      ON HinhAnh(MaHoSoMatTich) WHERE TrangThai = N'HoatDong';
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_HinhAnh_TinBao')
    CREATE INDEX IX_HinhAnh_TinBao           ON HinhAnh(MaTinBao) WHERE TrangThai = N'HoatDong';
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_HinhAnh_XacMinh')
    CREATE INDEX IX_HinhAnh_XacMinh          ON HinhAnh(MaXacMinh) WHERE TrangThai = N'HoatDong';
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_HinhAnh_TaiKhoanTaiLen')
    CREATE INDEX IX_HinhAnh_TaiKhoanTaiLen   ON HinhAnh(MaTaiKhoanTaiLen);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_HinhAnh_KhongNhayCam')
    CREATE INDEX IX_HinhAnh_KhongNhayCam     ON HinhAnh(LaNhayCam, TrangThai);
GO