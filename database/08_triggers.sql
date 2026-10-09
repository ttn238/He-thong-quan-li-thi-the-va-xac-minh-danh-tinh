USE QL_XACMINHTHITHE;
GO
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
  
/*  1. TIN BAO  */
CREATE OR ALTER TRIGGER trg_TinBao_KiemTra ON TinBao
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF UPDATE(ThoiGian)
       AND EXISTS (SELECT 1 FROM inserted WHERE ThoiGian > DATEADD(MINUTE, 5, SYSDATETIME()))
    BEGIN
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW 50301, N'Thoi gian tin bao khong duoc nam o tuong lai', 1;
    END

    IF UPDATE(TrangThai)
       AND EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.MaTin = i.MaTin
                   WHERE i.TrangThai <> d.TrangThai
                     AND fn_ChuyenTrangThaiHopLe('TIN_BAO', d.TrangThai, i.TrangThai) = 0)
    BEGIN
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW 50302, N'Khong cho phep chuyen trang thai tin bao nhu vay', 1;
    END
END
GO

/*  2. HO SO THI THE */
CREATE OR ALTER TRIGGER trg_HoSoThiThe_KiemTra ON HoSoThiThe
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF UPDATE(ThoiGianPhatHien)
       AND EXISTS (SELECT 1 FROM inserted WHERE ThoiGianPhatHien > DATEADD(MINUTE, 5, SYSDATETIME()))
    BEGIN
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW 50301, N'Thoi gian phat hien khong duoc nam o tuong lai', 1;
    END

    -- Ho so da dong: chi xem. Chi duoc mo lai (DongHoSo -> TiepTucLuuGiu, qua sp_MoLaiHoSoThiThe)
    IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.MaHoSoThiThe = i.MaHoSoThiThe
               WHERE d.TrangThai = N'DongHoSo' AND i.TrangThai <> N'TiepTucLuuGiu')
    BEGIN
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW 50303, N'Ho so da dong, chi xem. Muon sua phai mo lai bang sp_MoLaiHoSoThiThe', 1;
    END

    IF UPDATE(TrangThai)
       AND EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.MaHoSoThiThe = i.MaHoSoThiThe
                   WHERE i.TrangThai <> d.TrangThai
                     AND fn_ChuyenTrangThaiHopLe('HO_SO_THI_THE', d.TrangThai, i.TrangThai) = 0)
    BEGIN
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW 50302, N'Khong cho phep chuyen trang thai ho so thi the nhu vay', 1;
    END
END
GO

-- Dong ho so thi the => ho so nguoi mat tich da xac dinh chuyen DaDong
CREATE OR ALTER TRIGGER trg_HoSoThiThe_SauDong ON HoSoThiThe
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF UPDATE(TrangThai)
        UPDATE mt SET TrangThai = N'DaDong'
        FROM HoSoNguoiMatTich mt
        WHERE mt.TrangThai = N'DaTimThay'
          AND EXISTS (SELECT 1
                      FROM inserted i
                      JOIN deleted d    ON d.MaHoSoThiThe = i.MaHoSoThiThe
                      JOIN DoiSanh ds ON ds.MaHoSoThiThe = i.MaHoSoThiThe
                      JOIN XacMinh xm ON xm.MaDoiSanh = ds.MaDoiSanh AND xm.TrangThai = N'DaDuyet'
                      WHERE i.TrangThai = N'DongHoSo' AND d.TrangThai <> N'DongHoSo'
                        AND ds.MaHoSoMatTich = mt.MaHoSoMatTich);
END
GO

/*  3. HO SO NGUOI MAT TICH */
CREATE OR ALTER TRIGGER trg_HoSoMatTich_KiemTra ON HoSoNguoiMatTich
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF UPDATE(ThoiGianMatTich)
       AND EXISTS (SELECT 1 FROM inserted WHERE ThoiGianMatTich > DATEADD(MINUTE, 5, SYSDATETIME()))
    BEGIN
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW 50301, N'Thoi gian mat tich khong duoc nam o tuong lai', 1;
    END

    IF UPDATE(TrangThai)
       AND EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.MaHoSoMatTich = i.MaHoSoMatTich
                   WHERE i.TrangThai <> d.TrangThai
                     AND fn_ChuyenTrangThaiHopLe('HO_SO_MAT_TICH', d.TrangThai, i.TrangThai) = 0)
    BEGIN
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW 50302, N'Khong cho phep chuyen trang thai ho so mat tich nhu vay', 1;
    END
END
GO

/*  4. LUU TRU: suc chua ngan; xep thi the vao ngan => DangLuuGiu */
CREATE OR ALTER TRIGGER trg_LichSuLuuTru_KiemTra ON LichSuLuuTru
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    -- Khoa ngan dang xep de tranh 2 phien xep trung cung luc (gan bien, khong tra ket qua)
    DECLARE @khoa INT;
    SELECT TOP (1) @khoa = 1
    FROM ViTriLuuTru WITH (UPDLOCK, HOLDLOCK)
    WHERE MaViTri IN (SELECT MaViTri FROM inserted);

    IF EXISTS (SELECT 1 FROM inserted i JOIN HoSoThiThe h ON h.MaHoSoThiThe = i.MaHoSoThiThe
               WHERE h.TrangThai IN (N'DaBanGiao', N'DongHoSo'))
    BEGIN
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW 50304, N'Ho so da ban giao hoac da dong, khong xep vi tri duoc', 1;
    END

    IF EXISTS (SELECT 1
               FROM ViTriLuuTru v
               WHERE v.MaViTri IN (SELECT MaViTri FROM inserted)
                 AND v.SucChua < (SELECT COUNT(*)
                                  FROM v_ViTriHienTai vt
                                  JOIN HoSoThiThe h ON h.MaHoSoThiThe = vt.MaHoSoThiThe
                                  WHERE vt.MaViTri = v.MaViTri
                                    AND h.TrangThai NOT IN (N'DaBanGiao', N'DongHoSo')))
    BEGIN
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW 50305, N'Ngan da day, khong du suc chua', 1;
    END

    UPDATE h SET TrangThai = N'DangLuuGiu'
    FROM HoSoThiThe h
    JOIN inserted i ON i.MaHoSoThiThe = h.MaHoSoThiThe
    WHERE h.TrangThai = N'MoiTiepNhan';
END
GO

/*  5. DOI SANH: muc do theo nguong cau hinh, chuyen trang thai hop le */
CREATE OR ALTER TRIGGER trg_DoiSanh_KiemTra ON DoiSanh
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF UPDATE(TrangThai)
       AND EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.MaDoiSanh = i.MaDoiSanh
                   WHERE i.TrangThai <> d.TrangThai
                     AND fn_ChuyenTrangThaiHopLe('DOI_SANH', d.TrangThai, i.TrangThai) = 0)
    BEGIN
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW 50302, N'Khong cho phep chuyen trang thai doi sanh nhu vay', 1;
    END

    IF UPDATE(DiemDoiSanh)
        UPDATE ds SET MucDoPhuHop = fn_MucDo(i.DiemDoiSanh)
        FROM DoiSanh ds
        JOIN inserted i ON i.MaDoiSanh = ds.MaDoiSanh
        WHERE ISNULL(ds.MucDoPhuHop, N'') <> ISNULL(fn_MucDo(i.DiemDoiSanh), N'');
END
GO

/*  6. XAC MINH */
-- Tao phien xac minh: doi sanh phai o ChoXuLy; dong bo trang thai ho so
CREATE OR ALTER TRIGGER trg_XacMinh_Chen ON XacMinh
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM inserted i JOIN DoiSanh ds ON ds.MaDoiSanh = i.MaDoiSanh
               WHERE ds.TrangThai <> N'ChoXuLy')
    BEGIN
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW 50306, N'Cap doi sanh khong o trang thai ChoXuLy (da chuyen xac minh hoac da bi loai)', 1;
    END

    IF EXISTS (SELECT 1
               FROM inserted i
               JOIN DoiSanh ds          ON ds.MaDoiSanh = i.MaDoiSanh
               JOIN HoSoThiThe h        ON h.MaHoSoThiThe = ds.MaHoSoThiThe
               JOIN HoSoNguoiMatTich mt ON mt.MaHoSoMatTich = ds.MaHoSoMatTich
               WHERE h.TrangThai IN (N'DaXacDinh', N'DaBanGiao', N'DongHoSo')
                  OR mt.TrangThai IN (N'DaTimThay', N'DaDong'))
    BEGIN
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW 50307, N'Thi the hoac nguoi mat tich nay da duoc xac dinh', 1;
    END

    UPDATE ds SET TrangThai = N'ChuyenXacMinh'
    FROM DoiSanh ds JOIN inserted i ON i.MaDoiSanh = ds.MaDoiSanh;

    UPDATE h SET TrangThai = N'DangXacMinh'
    FROM HoSoThiThe h
    JOIN DoiSanh ds ON ds.MaHoSoThiThe = h.MaHoSoThiThe
    JOIN inserted i     ON i.MaDoiSanh = ds.MaDoiSanh
    WHERE h.TrangThai IN (N'MoiTiepNhan', N'DangLuuGiu', N'TiepTucLuuGiu');

    UPDATE mt SET TrangThai = N'CoHoSoNghiVan'
    FROM HoSoNguoiMatTich mt
    JOIN DoiSanh ds ON ds.MaHoSoMatTich = mt.MaHoSoMatTich
    JOIN inserted i     ON i.MaDoiSanh = ds.MaDoiSanh
    WHERE mt.TrangThai = N'DangTimKiem';
END
GO

-- Doi trang thai xac minh: dong bo ho so, thong bao
CREATE OR ALTER TRIGGER trg_XacMinh_CapNhat ON XacMinh
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT UPDATE(TrangThai) RETURN;

    IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.MaXacMinh = i.MaXacMinh
               WHERE i.TrangThai <> d.TrangThai
                 AND fn_ChuyenTrangThaiHopLe('XAC_MINH', d.TrangThai, i.TrangThai) = 0)
    BEGIN
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW 50302, N'Khong cho phep chuyen trang thai xac minh nhu vay', 1;
    END

    -- Moi thi the va moi nguoi mat tich chi duoc xac dinh (da duyet) mot lan
    IF EXISTS (SELECT 1
               FROM inserted i
               JOIN deleted d     ON d.MaXacMinh = i.MaXacMinh
               JOIN DoiSanh a ON a.MaDoiSanh = i.MaDoiSanh
               WHERE i.TrangThai = N'DaDuyet' AND d.TrangThai <> N'DaDuyet'
                 AND EXISTS (SELECT 1
                             FROM XacMinh x
                             JOIN DoiSanh b ON b.MaDoiSanh = x.MaDoiSanh
                             WHERE x.MaXacMinh <> i.MaXacMinh AND x.TrangThai = N'DaDuyet'
                               AND (b.MaHoSoThiThe = a.MaHoSoThiThe OR b.MaHoSoMatTich = a.MaHoSoMatTich)))
    BEGIN
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW 50308, N'Thi the hoac nguoi mat tich nay da duoc xac dinh o mot phien khac', 1;
    END

    /* --- Khong phu hop: loai cap nay, khong de xuat lai --- */
    UPDATE ds SET TrangThai = N'LoaiBo'
    FROM DoiSanh ds
    JOIN inserted i ON i.MaDoiSanh = ds.MaDoiSanh
    JOIN deleted d  ON d.MaXacMinh = i.MaXacMinh
    WHERE i.TrangThai = N'KhongPhuHop' AND d.TrangThai <> N'KhongPhuHop' AND ds.TrangThai <> N'LoaiBo';

    UPDATE h SET TrangThai = N'TiepTucLuuGiu'
    FROM HoSoThiThe h
    JOIN DoiSanh ds ON ds.MaHoSoThiThe = h.MaHoSoThiThe
    JOIN inserted i     ON i.MaDoiSanh = ds.MaDoiSanh
    JOIN deleted d      ON d.MaXacMinh = i.MaXacMinh
    WHERE i.TrangThai = N'KhongPhuHop' AND d.TrangThai <> N'KhongPhuHop'
      AND h.TrangThai = N'DangXacMinh'
      AND NOT EXISTS (SELECT 1 FROM XacMinh x JOIN DoiSanh b ON b.MaDoiSanh = x.MaDoiSanh
                      WHERE b.MaHoSoThiThe = h.MaHoSoThiThe AND x.MaXacMinh <> i.MaXacMinh
                        AND x.TrangThai <> N'KhongPhuHop');

    UPDATE mt SET TrangThai = N'DangTimKiem'
    FROM HoSoNguoiMatTich mt
    JOIN DoiSanh ds ON ds.MaHoSoMatTich = mt.MaHoSoMatTich
    JOIN inserted i     ON i.MaDoiSanh = ds.MaDoiSanh
    JOIN deleted d      ON d.MaXacMinh = i.MaXacMinh
    WHERE i.TrangThai = N'KhongPhuHop' AND d.TrangThai <> N'KhongPhuHop'
      AND mt.TrangThai = N'CoHoSoNghiVan'
      AND NOT EXISTS (SELECT 1 FROM XacMinh x JOIN DoiSanh b ON b.MaDoiSanh = x.MaDoiSanh
                      WHERE b.MaHoSoMatTich = mt.MaHoSoMatTich AND x.MaXacMinh <> i.MaXacMinh
                        AND x.TrangThai <> N'KhongPhuHop');

    /* --- Da duyet: xac dinh danh tinh --- */
    UPDATE h SET TrangThai = N'DaXacDinh'
    FROM HoSoThiThe h
    JOIN DoiSanh ds ON ds.MaHoSoThiThe = h.MaHoSoThiThe
    JOIN inserted i     ON i.MaDoiSanh = ds.MaDoiSanh
    JOIN deleted d      ON d.MaXacMinh = i.MaXacMinh
    WHERE i.TrangThai = N'DaDuyet' AND d.TrangThai <> N'DaDuyet' AND h.TrangThai = N'DangXacMinh';

    UPDATE mt SET TrangThai = N'DaTimThay'
    FROM HoSoNguoiMatTich mt
    JOIN DoiSanh ds ON ds.MaHoSoMatTich = mt.MaHoSoMatTich
    JOIN inserted i     ON i.MaDoiSanh = ds.MaDoiSanh
    JOIN deleted d      ON d.MaXacMinh = i.MaXacMinh
    WHERE i.TrangThai = N'DaDuyet' AND d.TrangThai <> N'DaDuyet' AND mt.TrangThai = N'CoHoSoNghiVan';

    /* --- Thong bao toi thieu (chi ma phien, khong thong tin nhay cam) --- */
    -- Co ket luan cho duyet => bao cho tat ca tai khoan co quyen duyet (tru nguoi thuc hien)
    INSERT ThongBao (NoiDung, MaTaiKhoan)
    SELECT CONCAT(N'Kết luận xác minh XM-', i.MaXacMinh, N' đang chờ duyệt'), tk.MaTaiKhoan
    FROM inserted i
    JOIN deleted d ON d.MaXacMinh = i.MaXacMinh
    JOIN TaiKhoanNguoiDung tk ON tk.MaTaiKhoan <> i.MaTaiKhoanThucHien
    WHERE i.TrangThai = N'ChoDuyet' AND d.TrangThai <> N'ChoDuyet'
      AND fn_CoQuyen(tk.MaTaiKhoan, 'VERIFICATION', 'APPROVE') = 1;

    -- Duyet / tu choi duyet => bao cho nguoi thuc hien
    INSERT ThongBao (NoiDung, MaTaiKhoan)
    SELECT CASE i.TrangThai
                WHEN N'DaDuyet' THEN CONCAT(N'Kết luận xác minh XM-', i.MaXacMinh, N' đã được duyệt')
                ELSE CONCAT(N'Kết luận xác minh XM-', i.MaXacMinh, N' bị từ chối duyệt')
           END,
           i.MaTaiKhoanThucHien
    FROM inserted i
    JOIN deleted d ON d.MaXacMinh = i.MaXacMinh
    WHERE i.TrangThai IN (N'DaDuyet', N'TuChoiDuyet') AND d.TrangThai <> i.TrangThai;
END
GO

/*  7. PHE DUYET: nguoi duyet phai co quyen va KHAC nguoi thuc hien xac minh */
CREATE OR ALTER TRIGGER trg_PheDuyet_KiemTra ON PheDuyet
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Quyet dinh Duyet da chot thi khong duoc sua (TuChoi thi duoc quyet dinh lai sau khi trinh duyet lai)
    IF EXISTS (SELECT 1 FROM deleted WHERE TrangThai = N'Duyet')
    BEGIN
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW 50309, N'Phe duyet da chot, khong duoc sua', 1;
    END

    IF EXISTS (SELECT 1 FROM inserted i JOIN XacMinh x ON x.MaXacMinh = i.MaXacMinh
               WHERE i.TrangThai IN (N'Duyet', N'TuChoi') AND x.TrangThai <> N'ChoDuyet')
    BEGIN
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW 50310, N'Chi phe duyet ket luan dang o trang thai ChoDuyet', 1;
    END

    IF EXISTS (SELECT 1 FROM inserted i
               WHERE i.TrangThai IN (N'Duyet', N'TuChoi')
                 AND fn_CoQuyen(i.MaNguoiDuyet, 'VERIFICATION', 'APPROVE') = 0)
    BEGIN
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW 50311, N'Nguoi duyet khong co quyen duyet hoac tai khoan khong hoat dong', 1;
    END

    IF EXISTS (SELECT 1 FROM inserted i JOIN XacMinh x ON x.MaXacMinh = i.MaXacMinh
               WHERE i.TrangThai IN (N'Duyet', N'TuChoi') AND i.MaNguoiDuyet = x.MaTaiKhoanThucHien)
    BEGIN
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW 50312, N'Nguoi duyet phai khac nguoi thuc hien xac minh', 1;
    END

    UPDATE x
       SET TrangThai = CASE i.TrangThai WHEN N'Duyet' THEN N'DaDuyet' ELSE N'TuChoiDuyet' END
    FROM XacMinh x
    JOIN inserted i ON i.MaXacMinh = x.MaXacMinh
    WHERE i.TrangThai IN (N'Duyet', N'TuChoi');
END
GO

/* 8. BAN GIAO */
-- Chi lap khi ket luan da duyet; nguoi nhan phai la nguoi than cua dung ho so mat tich; nguoi giao co quyen
CREATE OR ALTER TRIGGER trg_BanGiao_Chen ON BanGiao
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM inserted i JOIN XacMinh x ON x.MaXacMinh = i.MaXacMinh
               WHERE x.TrangThai <> N'DaDuyet')
    BEGIN
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW 50313, N'Chi lap ban giao khi ket luan xac minh da duoc duyet', 1;
    END

    IF EXISTS (SELECT 1
               FROM inserted i
               JOIN XacMinh x   ON x.MaXacMinh = i.MaXacMinh
               JOIN DoiSanh ds  ON ds.MaDoiSanh = x.MaDoiSanh
               JOIN HoSoThiThe h ON h.MaHoSoThiThe = ds.MaHoSoThiThe
               WHERE h.TrangThai <> N'DaXacDinh')
    BEGIN
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW 50314, N'Ho so thi the phai o trang thai DaXacDinh moi duoc ban giao', 1;
    END

    IF EXISTS (SELECT 1
               FROM inserted i
               JOIN XacMinh x  ON x.MaXacMinh = i.MaXacMinh
               JOIN DoiSanh ds ON ds.MaDoiSanh = x.MaDoiSanh
               JOIN NguoiThan nt ON nt.MaNguoiThan = i.MaNguoiThan
               WHERE nt.MaHoSoMatTich <> ds.MaHoSoMatTich)
    BEGIN
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW 50315, N'Nguoi nhan khong phai nguoi than cua ho so mat tich nay', 1;
    END

    IF EXISTS (SELECT 1 FROM inserted i WHERE fn_CoQuyen(i.MaNguoiGiao, 'HANDOVER', 'HANDOVER') = 0)
    BEGIN
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW 50316, N'Nguoi giao khong co quyen giao thi the', 1;
    END
END
GO

-- Doi trang thai ban giao: chuyen hop le; hoan tat => ho so thi the DaBanGiao (vi tri tu duoc giai phong)
CREATE OR ALTER TRIGGER trg_BanGiao_CapNhat ON BanGiao
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT UPDATE(TrangThai) RETURN;

    IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.MaBanGiao = i.MaBanGiao
               WHERE i.TrangThai <> d.TrangThai
                 AND fn_ChuyenTrangThaiHopLe('BAN_GIAO', d.TrangThai, i.TrangThai) = 0)
    BEGIN
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW 50302, N'Khong cho phep chuyen trang thai ban giao nhu vay', 1;
    END

    UPDATE h SET TrangThai = N'DaBanGiao'
    FROM HoSoThiThe h
    JOIN DoiSanh ds ON ds.MaHoSoThiThe = h.MaHoSoThiThe
    JOIN XacMinh x  ON x.MaDoiSanh = ds.MaDoiSanh
    JOIN inserted i     ON i.MaXacMinh = x.MaXacMinh
    JOIN deleted d      ON d.MaBanGiao = i.MaBanGiao
    WHERE i.TrangThai = N'DaXacNhan' AND d.TrangThai <> N'DaXacNhan' AND h.TrangThai = N'DaXacDinh';
END
GO

/*  9. THAM SO: tong trong so doi sanh (MATCH_W_*) phai bang 100
   Neu sua nhieu trong so, dung MOT cau UPDATE/INSERT de tong dung ngay luc ket thuc cau lenh. */
CREATE OR ALTER TRIGGER trg_ThamSo_TrongSo ON ThamSoHeThong
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted WHERE TenThamSo LIKE 'MATCH[_]W[_]%')
       OR EXISTS (SELECT 1 FROM deleted WHERE TenThamSo LIKE 'MATCH[_]W[_]%')
    BEGIN
        DECLARE @tong DECIMAL(10,2) =
            (SELECT SUM(TRY_CAST(GiaTri AS DECIMAL(10,2))) FROM ThamSoHeThong WHERE TenThamSo LIKE 'MATCH[_]W[_]%');
        IF @tong IS NOT NULL AND @tong <> 100
        BEGIN
            IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
            THROW 50317, N'Tong trong so doi sanh (MATCH_W_*) phai bang 100', 1;
        END
    END
END
GO

/*  10. NHAT KY CHI THEM MOI (khong sua / xoa)
   Luu y: TRUNCATE TABLE khong the chan bang trigger; neu can thi DENY ALTER tren bang nay.  */
CREATE OR ALTER TRIGGER trg_NhatKy_ChiThem ON NhatKyHeThong
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    THROW 50318, N'Bang NhatKyHeThong chi cho phep them moi (append-only)', 1;
END
GO

/*  11. GHI NHAT KY TU DONG (audit)
   Ung dung dat nguoi thao tac: EXEC sys.sp_set_session_context N'user_id', <MaTaiKhoan>
   (cac thu tuc o file 07 da tu dat). Tao bang dynamic SQL tu mot mau chung.
   Ghi: hanh dong, bang#khoa chinh, va (neu co) trang thai cu -> moi. */
DECLARE @Bang SYSNAME, @Khoa SYSNAME, @CoTT BIT, @sql NVARCHAR(MAX);

DECLARE @Mau NVARCHAR(MAX) = N'
CREATE OR ALTER TRIGGER trg_Audit_{B} ON {B}
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @nguoi BIGINT = fn_NguoiThaoTac();

    IF EXISTS (SELECT 1 FROM inserted) AND EXISTS (SELECT 1 FROM deleted)
        INSERT NhatKyHeThong (HanhDong, DoiTuongTacDong, MaTaiKhoan)
        SELECT N''UPDATE'', CONCAT(N''{B}#'', i.{K}{TT}), @nguoi
        FROM inserted i JOIN deleted d ON d.{K} = i.{K};
    ELSE IF EXISTS (SELECT 1 FROM inserted)
        INSERT NhatKyHeThong (HanhDong, DoiTuongTacDong, MaTaiKhoan)
        SELECT N''INSERT'', CONCAT(N''{B}#'', {K}), @nguoi FROM inserted;
    ELSE
        INSERT NhatKyHeThong (HanhDong, DoiTuongTacDong, MaTaiKhoan)
        SELECT N''DELETE'', CONCAT(N''{B}#'', {K}), @nguoi FROM deleted;
END';

DECLARE @PhanTrangThai NVARCHAR(400) =
    N', CASE WHEN d.TrangThai <> i.TrangThai THEN CONCAT(N'' '', d.TrangThai, N'' -> '', i.TrangThai) ELSE N'''' END';

DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
    SELECT Bang, Khoa, CoTT
    FROM (VALUES
        (N'TinBao',            N'MaTin',          1),
        (N'HoSoThiThe',        N'MaHoSoThiThe',   1),
        (N'HoSoNguoiMatTich',  N'MaHoSoMatTich',  1),
        (N'NguoiThan',         N'MaNguoiThan',    0),
        (N'TepTin',            N'MaTep',          0),
        (N'DoVatCaNhan',       N'MaDoVat',        0),
        (N'LichSuLuuTru',      N'MaLichSu',       0),
        (N'DoiSanh',           N'MaDoiSanh',      1),
        (N'XacMinh',           N'MaXacMinh',      1),
        (N'PheDuyet',          N'MaPheDuyet',     1),
        (N'BanGiao',           N'MaBanGiao',      1),
        (N'TaiKhoanNguoiDung', N'MaTaiKhoan',     1)
    ) v (Bang, Khoa, CoTT);

OPEN cur;
FETCH NEXT FROM cur INTO @Bang, @Khoa, @CoTT;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @sql = REPLACE(REPLACE(REPLACE(@Mau, N'{B}', @Bang), N'{K}', @Khoa),
                       N'{TT}', CASE WHEN @CoTT = 1 THEN @PhanTrangThai ELSE N'' END);
    EXEC (@sql);
    FETCH NEXT FROM cur INTO @Bang, @Khoa, @CoTT;
END
CLOSE cur;
DEALLOCATE cur;
GO