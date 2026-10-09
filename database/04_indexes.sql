USE QL_XACMINHTHITHE;
GO

CREATE INDEX IX_TaiKhoan_MaVaiTro        ON TaiKhoanNguoiDung(MaVaiTro);
CREATE INDEX IX_VaiTroQuyen_MaQuyen      ON VaiTro_Quyen(MaQuyen);
CREATE INDEX IX_TinBao_MaLoaiTinBao      ON TinBao(MaLoaiTinBao);
CREATE INDEX IX_TinBao_TiepNhan          ON TinBao(MaTaiKhoanTiepNhan);
CREATE INDEX IX_TepTinBao_TinBao ON TepTinBao(MaTinBao);
CREATE INDEX IX_HoSoThiThe_MaTin         ON HoSoThiThe(MaTin);
CREATE INDEX IX_NguoiThan_HoSoMatTich    ON NguoiThan(MaHoSoMatTich);
CREATE INDEX IX_TepTin_HoSoThiThe        ON TepTin(MaHoSoThiThe);
CREATE INDEX IX_TepTin_HoSoMatTich       ON TepTin(MaHoSoMatTich);
CREATE INDEX IX_KetQuaOCR_MaTep          ON KetQuaOCR(MaTep);
CREATE INDEX IX_PhuongTien_MaKetQua      ON PhuongTien(MaKetQua);
CREATE INDEX IX_PhuongTien_BienSo        ON PhuongTien(BienSo);
CREATE INDEX IX_DoVat_HoSoThiThe         ON DoVatCaNhan(MaHoSoThiThe);
CREATE INDEX IX_DoVat_LoaiDoVat          ON DoVatCaNhan(MaLoaiDoVat);
CREATE INDEX IX_LichSuLuuTru_HoSo        ON LichSuLuuTru(MaHoSoThiThe, ThoiGian DESC);
CREATE INDEX IX_LichSuLuuTru_ViTri       ON LichSuLuuTru(MaViTri);
CREATE INDEX IX_DoiSanh_HoSoThiThe       ON DoiSanh(MaHoSoThiThe);
CREATE INDEX IX_DoiSanh_HoSoMatTich      ON DoiSanh(MaHoSoMatTich);
CREATE INDEX IX_DoiSanh_TaiKhoanYeuCau   ON DoiSanh(MaTaiKhoanYeuCau);
CREATE INDEX IX_XacMinh_DoiSanh          ON XacMinh(MaDoiSanh);
CREATE INDEX IX_XacMinh_TaiKhoan         ON XacMinh(MaTaiKhoanThucHien);
CREATE INDEX IX_PheDuyet_NguoiDuyet      ON PheDuyet(MaNguoiDuyet);
CREATE INDEX IX_BanGiao_XacMinh          ON BanGiao(MaXacMinh);
CREATE INDEX IX_BanGiao_NguoiThan        ON BanGiao(MaNguoiThan);
CREATE INDEX IX_BanGiao_NguoiGiao        ON BanGiao(MaNguoiGiao);
CREATE INDEX IX_NhatKy_TaiKhoan_ThoiGian ON NhatKyHeThong(MaTaiKhoan, ThoiGian DESC);
CREATE INDEX IX_ThongBao_TaiKhoan        ON ThongBao(MaTaiKhoan, TrangThaiDoc);
GO