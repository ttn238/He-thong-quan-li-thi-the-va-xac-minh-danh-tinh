USE QL_XACMINHTHITHE;
GO
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
 
-- Vi tri HIEN TAI cua moi ho so thi the = dong lich su moi nhat
CREATE OR ALTER VIEW v_ViTriHienTai
AS
SELECT x.MaHoSoThiThe, x.MaViTri, x.ThoiGian AS ThoiGianXep
FROM (SELECT ls.*,
             ROW_NUMBER() OVER (PARTITION BY ls.MaHoSoThiThe ORDER BY ls.ThoiGian DESC, ls.MaLichSu DESC) AS rn
      FROM LichSuLuuTru ls) x
WHERE x.rn = 1;
GO

-- Tinh trang tung ngan: Trong / DangLuuGiu (thi the da ban giao hoac dong ho so thi khong tinh la dang chiem cho)
CREATE OR ALTER VIEW v_TinhTrangViTri
AS
SELECT v.MaViTri, v.Khu, v.Tu, v.Ngan, v.SucChua,
       ISNULL(c.SoLuong, 0)                AS DangChua,
       v.SucChua - ISNULL(c.SoLuong, 0)    AS ConTrong,
       CASE WHEN ISNULL(c.SoLuong, 0) >= v.SucChua THEN N'DangLuuGiu' ELSE N'Trong' END AS TrangThaiViTri
FROM ViTriLuuTru v
LEFT JOIN (SELECT vt.MaViTri, COUNT(*) AS SoLuong
           FROM v_ViTriHienTai vt
           JOIN HoSoThiThe h ON h.MaHoSoThiThe = vt.MaHoSoThiThe
           WHERE h.TrangThai NOT IN (N'DaBanGiao', N'DongHoSo')
           GROUP BY vt.MaViTri) c ON c.MaViTri = v.MaViTri;
GO

-- Cap da xac dinh danh tinh (ket luan da duoc duyet)
CREATE OR ALTER VIEW v_CapDaXacDinh
AS
SELECT xm.MaXacMinh, ds.MaHoSoThiThe, ds.MaHoSoMatTich, mt.HoTen AS HoTenNguoiMatTich,
       xm.MaTaiKhoanThucHien, pd.MaNguoiDuyet, pd.ThoiGian AS ThoiGianDuyet
FROM XacMinh xm
JOIN DoiSanh ds         ON ds.MaDoiSanh = xm.MaDoiSanh
JOIN HoSoNguoiMatTich mt ON mt.MaHoSoMatTich = ds.MaHoSoMatTich
LEFT JOIN PheDuyet pd   ON pd.MaXacMinh = xm.MaXacMinh AND pd.TrangThai = N'Duyet'
WHERE xm.TrangThai = N'DaDuyet';
GO

-- Nha xac chi thay phan duoc phan quyen: khong co dac diem co the, khuon mat, dia diem chi tiet
CREATE OR ALTER VIEW v_HoSoThiTheNhaXac
AS
SELECT h.MaHoSoThiThe, h.TrangThai, h.ThoiGianPhatHien, h.GioiTinh, h.TuoiUocLuong,
       vt.MaViTri, v.Khu, v.Tu, v.Ngan
FROM HoSoThiThe h
LEFT JOIN v_ViTriHienTai vt ON vt.MaHoSoThiThe = h.MaHoSoThiThe
LEFT JOIN ViTriLuuTru v     ON v.MaViTri = vt.MaViTri;
GO

-- Doi sanh kem ten de hien thi tren giao dien
CREATE OR ALTER VIEW v_DoiSanhChiTiet
AS
SELECT ds.MaDoiSanh, ds.MaHoSoThiThe, ds.MaHoSoMatTich, mt.HoTen AS HoTenNguoiMatTich,
       ds.DiemDoiSanh, ds.MucDoPhuHop, ds.TrangThai, ds.TieuChi,
       ds.ThoiGianYeuCau, tk.HoTen AS NguoiYeuCau
FROM DoiSanh ds
JOIN HoSoNguoiMatTich mt  ON mt.MaHoSoMatTich = ds.MaHoSoMatTich
JOIN TaiKhoanNguoiDung tk ON tk.MaTaiKhoan = ds.MaTaiKhoanYeuCau;
GO

-- Bang dieu khien tong quan
CREATE OR ALTER VIEW v_BangDieuKhien
AS
SELECT
    (SELECT COUNT(*) FROM TinBao WHERE TrangThai = N'MoiTiepNhan')                                  AS TinBaoChoTiepNhan,
    (SELECT COUNT(*) FROM HoSoThiThe WHERE TrangThai NOT IN (N'DaBanGiao', N'DongHoSo'))           AS ThiTheDangXuLy,
    (SELECT COUNT(*) FROM HoSoNguoiMatTich WHERE TrangThai IN (N'DangTimKiem', N'CoHoSoNghiVan'))  AS MatTichDangTim,
    (SELECT COUNT(*) FROM HoSoThiThe WHERE TrangThai IN (N'DaXacDinh', N'DaBanGiao', N'DongHoSo')) AS ThiTheDaXacDinh,
    (SELECT COUNT(*) FROM HoSoThiThe)                                                               AS TongThiThe,
    (SELECT COUNT(*) FROM v_TinhTrangViTri WHERE TrangThaiViTri = N'Trong')                         AS ViTriTrong,
    (SELECT COUNT(*) FROM XacMinh WHERE TrangThai = N'ChoDuyet')                                    AS XacMinhChoDuyet;
GO