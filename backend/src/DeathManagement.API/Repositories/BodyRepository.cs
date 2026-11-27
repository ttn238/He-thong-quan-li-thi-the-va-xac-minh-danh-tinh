using System.Text.RegularExpressions;
using DeathManagement.API.Data;
using DeathManagement.API.DTOs.Body;
using DeathManagement.API.Entities;
using DeathManagement.API.Repositories.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace DeathManagement.API.Repositories;

/// <summary>
/// Triển khai repository hồ sơ thi thể, gọi Stored Procedures SQL Server và EF Core.
/// </summary>
public class BodyRepository(ApplicationDbContext db) : IBodyRepository
{
    private static readonly Regex StatusArrowRegex = new(@"(\w+)\s*->\s*(\w+)", RegexOptions.Compiled);

    /// <inheritdoc />
    public async Task<long> TaoHoSoAsync(CreateBodyRequest request, long maTaiKhoan)
    {
        // sp_LapHoSoThiThe kiểm tra quyền BODY:CREATE ở tầng DB
        var pMaHS = new SqlParameter("@MaHoSoThiThe", System.Data.SqlDbType.BigInt)
        {
            Direction = System.Data.ParameterDirection.Output
        };

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            EXEC sp_LapHoSoThiThe
                @MaTaiKhoan        = {maTaiKhoan},
                @ThoiGianPhatHien  = {request.ThoiGianPhatHien},
                @DiaDiem           = {request.DiaDiem},
                @GioiTinh          = {request.GioiTinh},
                @TuoiUocLuong      = {request.TuoiUocLuong},
                @ChieuCao          = {request.ChieuCao},
                @DacDiemCoThe      = {request.DacDiemCoThe},
                @MaTinBao          = {request.MaTinBao},
                @MaHoSoThiThe      = {pMaHS} OUT
            """);

        return (long)(pMaHS.Value ?? 0L);
    }

    /// <inheritdoc />
    public async Task<Body?> LayTheoMaAsync(long maHoSoThiThe)
    {
        return await db.Set<Body>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.MaHoSoThiThe == maHoSoThiThe);
    }

    /// <inheritdoc />
    public async Task<BodyDto?> LayChiTietDtoAsync(long maHoSoThiThe, bool nhaXac)
    {
        if (nhaXac)
        {
            return await db.Database.SqlQuery<BodyDto>($"""
                SELECT MaHoSoThiThe, TrangThai, ThoiGianPhatHien, GioiTinh, TuoiUocLuong,
                       MaViTri, Khu, Tu, Ngan,
                       CAST(NULL AS nvarchar(255)) AS DiaDiem,
                       CAST(NULL AS decimal(5,2)) AS ChieuCao,
                       CAST(NULL AS nvarchar(max)) AS DacDiemCoThe,
                       CAST(NULL AS int) AS MaTinBao
                FROM dbo.v_HoSoThiTheNhaXac
                WHERE MaHoSoThiThe = {maHoSoThiThe}
                """).FirstOrDefaultAsync();
        }

        return await db.Database.SqlQuery<BodyDto>($"""
            SELECT h.MaHoSoThiThe, h.ThoiGianPhatHien, h.DiaDiem, h.TrangThai,
                   h.GioiTinh, h.TuoiUocLuong, h.ChieuCao, h.DacDiemCoThe, h.MaTinBao,
                   vt.MaViTri, v.Khu, v.Tu, v.Ngan
            FROM dbo.HoSoThiThe h
            LEFT JOIN dbo.v_ViTriHienTai vt ON vt.MaHoSoThiThe = h.MaHoSoThiThe
            LEFT JOIN dbo.ViTriLuuTru v ON v.MaViTri = vt.MaViTri
            WHERE h.MaHoSoThiThe = {maHoSoThiThe}
            """).FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<BodyDetailDto?> LayChiTietDayDuAsync(
        long maHoSoThiThe,
        long maTaiKhoan,
        bool nhaXac,
        bool coQuyenXemDoVat,
        bool coQuyenXemHinhAnh,
        bool coQuyenXemDoiSanh)
    {
        var baseDto = await LayChiTietDtoAsync(maHoSoThiThe, nhaXac);
        if (baseDto == null)
            return null;

        var detail = new BodyDetailDto
        {
            MaHoSoThiThe       = baseDto.MaHoSoThiThe,
            ThoiGianPhatHien   = baseDto.ThoiGianPhatHien,
            DiaDiem            = nhaXac ? null : baseDto.DiaDiem,
            TrangThai          = baseDto.TrangThai,
            GioiTinh           = baseDto.GioiTinh,
            TuoiUocLuong       = baseDto.TuoiUocLuong,
            ChieuCao           = nhaXac ? null : baseDto.ChieuCao,
            DacDiemCoThe       = nhaXac ? null : baseDto.DacDiemCoThe,
            MaTinBao           = nhaXac ? null : baseDto.MaTinBao,
            MaViTri            = baseDto.MaViTri,
            Khu                = baseDto.Khu,
            Tu                 = baseDto.Tu,
            Ngan               = baseDto.Ngan,
            CheDoHanCheNhaXac  = nhaXac,
            CoQuyenXemDoVat    = coQuyenXemDoVat,
            CoQuyenXemHinhAnh  = coQuyenXemHinhAnh,
            CoQuyenXemDoiSanh  = coQuyenXemDoiSanh && !nhaXac
        };

        // 1. Tin báo gốc (ẩn hoàn toàn với vai trò Nhà xác)
        if (!nhaXac && baseDto.MaTinBao.HasValue)
        {
            var maTinBao = baseDto.MaTinBao.Value;
            detail.TinBao = await db.Database.SqlQuery<BodyIncidentSummaryDto>($"""
                SELECT tb.MaTinBao, tb.MaTin, ltb.TenLoaiTinBao, tb.TieuDe, tb.NoiDung,
                       tb.ThoiGianXayRa, tb.DiaDiem, tb.NguonTin, tb.HoTenNguoiBao,
                       tb.SoDienThoaiNguoiBao, tb.EmailNguoiBao, tb.MucDoUuTien,
                       tb.TrangThai, tb.GhiChuXuLy, tb.NgayTiepNhan
                FROM dbo.TinBao tb
                LEFT JOIN dbo.LoaiTinBao ltb ON ltb.MaLoaiTinBao = tb.MaLoaiTinBao
                WHERE tb.MaTinBao = {maTinBao}
                """).FirstOrDefaultAsync();
        }

        // 2. Vị trí ngăn lạnh hiện tại (v_ViTriHienTai + ViTriLuuTru) & lịch sử lưu trữ (LichSuLuuTru)
        detail.ViTriHienTai = await db.Database.SqlQuery<BodyStorageCurrentDto>($"""
            SELECT vt.MaViTri, v.Khu, v.Tu, v.Ngan, v.SucChua, vt.ThoiGianXep
            FROM dbo.v_ViTriHienTai vt
            JOIN dbo.ViTriLuuTru v ON v.MaViTri = vt.MaViTri
            WHERE vt.MaHoSoThiThe = {maHoSoThiThe}
            """).FirstOrDefaultAsync();

        detail.LichSuLuuTru = await db.Database.SqlQuery<BodyStorageHistoryDto>($"""
            SELECT ls.MaLichSu, ls.ThoiGian, ls.MaViTri, v.Khu, v.Tu, v.Ngan
            FROM dbo.LichSuLuuTru ls
            JOIN dbo.ViTriLuuTru v ON v.MaViTri = ls.MaViTri
            WHERE ls.MaHoSoThiThe = {maHoSoThiThe}
            ORDER BY ls.ThoiGian DESC, ls.MaLichSu DESC
            """).ToListAsync();

        // 3. Danh sách đồ vật cá nhân (DoVatCaNhan + LoaiDoVat)
        if (coQuyenXemDoVat)
        {
            detail.DoVatCaNhan = await db.Database.SqlQuery<BodyPersonalItemDto>($"""
                SELECT dv.MaDoVat, dv.Ten, dv.MaLoaiDoVat, ldv.TenLoai AS TenLoaiDoVat,
                       dv.MoTa, dv.TinhTrang
                FROM dbo.DoVatCaNhan dv
                JOIN dbo.LoaiDoVat ldv ON ldv.MaLoaiDoVat = dv.MaLoaiDoVat
                WHERE dv.MaHoSoThiThe = {maHoSoThiThe}
                ORDER BY dv.MaDoVat ASC
                """).ToListAsync();
        }

        // 4. Danh sách hình ảnh gọi trực tiếp thủ tục sp_LayHinhAnhTheoHoSo
        if (coQuyenXemHinhAnh)
        {
            detail.HinhAnh = await LayHinhAnhQuaSpAsync(maTaiKhoan, maHoSoThiThe);
        }

        // 5. Danh sách kết quả đối sánh từ view v_DoiSanhChiTiet
        if (coQuyenXemDoiSanh && !nhaXac)
        {
            detail.KetQuaDoiSanh = await db.Database.SqlQuery<BodyMatchResultDto>($"""
                SELECT MaDoiSanh, MaHoSoThiThe, MaHoSoMatTich, HoTenNguoiMatTich,
                       DiemDoiSanh, MucDoPhuHop, TrangThai, TieuChi,
                       ThoiGianYeuCau, NguoiYeuCau
                FROM dbo.v_DoiSanhChiTiet
                WHERE MaHoSoThiThe = {maHoSoThiThe}
                ORDER BY DiemDoiSanh DESC, ThoiGianYeuCau DESC
                """).ToListAsync();
        }

        // 6. Lịch sử thay đổi trạng thái (NhatKyHeThong)
        detail.LichSuTrangThai = await LayLichSuTrangThaiAsync(maHoSoThiThe);

        return detail;
    }

    private async Task<List<BodyImageItemDto>> LayHinhAnhQuaSpAsync(long maTaiKhoan, long maHoSoThiThe)
    {
        var items = new List<BodyImageItemDto>();
        var conn = db.Database.GetDbConnection();
        var wasOpen = conn.State == System.Data.ConnectionState.Open;
        if (!wasOpen) await conn.OpenAsync();
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "dbo.sp_LayHinhAnhTheoHoSo";
            cmd.CommandType = System.Data.CommandType.StoredProcedure;

            var pAcc = cmd.CreateParameter();
            pAcc.ParameterName = "@MaTaiKhoanTruyCap";
            pAcc.Value = maTaiKhoan;
            cmd.Parameters.Add(pAcc);

            var pBody = cmd.CreateParameter();
            pBody.ParameterName = "@MaHoSoThiThe";
            pBody.Value = maHoSoThiThe;
            cmd.Parameters.Add(pBody);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(new BodyImageItemDto
                {
                    MaHinhAnh      = reader.GetInt64(0),
                    TenGoc         = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                    DuongDan       = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    LoaiHinhAnh    = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    DinhDang       = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                    KichThuoc      = reader.IsDBNull(5) ? 0L : reader.GetInt64(5),
                    LaNhayCam      = !reader.IsDBNull(6) && reader.GetBoolean(6),
                    MoTa           = reader.IsDBNull(7) ? null : reader.GetString(7),
                    ThoiGianTaiLen = reader.IsDBNull(8) ? DateTime.MinValue : reader.GetDateTime(8),
                    NguoiTaiLen    = reader.IsDBNull(9) ? "Hệ thống" : reader.GetString(9)
                });
            }
        }
        finally
        {
            if (!wasOpen && conn.State == System.Data.ConnectionState.Open)
                await conn.CloseAsync();
        }

        return items;
    }

    /// <inheritdoc />
    public async Task<(List<BodyDto> Items, int Total)> LayDanhSachAsync(
        string? tuKhoa, string? trangThai, int trang, int soLuong)
        => await LayDanhSachAsync(new BodyFilterRequest
        { TuKhoa = tuKhoa, TrangThai = trangThai, Trang = trang, SoLuong = soLuong }, false);

    public async Task<(List<BodyDto> Items, int Total)> LayDanhSachAsync(BodyFilterRequest filter, bool nhaXac)
    {
        // Query the restricted view before filtering so hidden fields cannot leak through search counts.
        IQueryable<BodyDto> query = nhaXac
            ? db.Database.SqlQuery<BodyDto>($"""
                SELECT MaHoSoThiThe, TrangThai, ThoiGianPhatHien, GioiTinh, TuoiUocLuong,
                       MaViTri, Khu, Tu, Ngan,
                       CAST(NULL AS nvarchar(255)) AS DiaDiem,
                       CAST(NULL AS decimal(5,2)) AS ChieuCao,
                       CAST(NULL AS nvarchar(max)) AS DacDiemCoThe,
                       CAST(NULL AS int) AS MaTinBao
                FROM dbo.v_HoSoThiTheNhaXac
                """)
            : db.Database.SqlQuery<BodyDto>($"""
                SELECT h.MaHoSoThiThe, h.ThoiGianPhatHien, h.DiaDiem, h.TrangThai,
                       h.GioiTinh, h.TuoiUocLuong, h.ChieuCao, h.DacDiemCoThe, h.MaTinBao,
                       vt.MaViTri, v.Khu, v.Tu, v.Ngan
                FROM dbo.HoSoThiThe h
                LEFT JOIN dbo.v_ViTriHienTai vt ON vt.MaHoSoThiThe = h.MaHoSoThiThe
                LEFT JOIN dbo.ViTriLuuTru v ON v.MaViTri = vt.MaViTri
                """);

        if (!string.IsNullOrWhiteSpace(filter.TrangThai))
            query = query.Where(x => x.TrangThai == filter.TrangThai);
        if (!string.IsNullOrWhiteSpace(filter.GioiTinh))
            query = query.Where(x => x.GioiTinh == filter.GioiTinh);
        if (filter.TuNgay.HasValue)
            query = query.Where(x => x.ThoiGianPhatHien >= filter.TuNgay.Value);
        if (filter.DenNgay.HasValue)
            query = query.Where(x => x.ThoiGianPhatHien <= filter.DenNgay.Value);

        if (!string.IsNullOrWhiteSpace(filter.TuKhoa))
        {
            var kw = filter.TuKhoa.Trim();
            query = query.Where(x =>
                x.MaHoSoThiThe.ToString().Contains(kw) ||
                (x.DiaDiem != null && x.DiaDiem.Contains(kw)) ||
                (x.DacDiemCoThe != null && x.DacDiemCoThe.Contains(kw)));
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.MaHoSoThiThe)
            .Skip(checked((filter.Trang - 1) * filter.SoLuong))
            .Take(filter.SoLuong)
            .ToListAsync();

        return (items, total);
    }

    /// <inheritdoc />
    public async Task CapNhatAsync(long maHoSoThiThe, UpdateBodyRequest request, long maTaiKhoan)
    {
        var body = await db.Set<Body>().FindAsync(maHoSoThiThe)
                   ?? throw new KeyNotFoundException($"Không tìm thấy hồ sơ thi thể #{maHoSoThiThe}.");

        // Nếu chỉ cập nhật thông tin trên hồ sơ đã đóng (mà không phải mở lại sang TiepTucLuuGiu) thì chặn
        var isReopening = !string.IsNullOrWhiteSpace(request.TrangThai)
                          && body.TrangThai == Body.StatusDongHoSo
                          && request.TrangThai.Trim() == Body.StatusTiepTucLuuGiu;

        if (body.TrangThai == Body.StatusDongHoSo && !isReopening)
            throw new InvalidOperationException("Hồ sơ đã đóng. Vui lòng mở lại hồ sơ trước khi sửa đổi.");

        // 1. Nếu có yêu cầu chuyển trạng thái đi kèm thì xử lý chuyển trạng thái trước
        if (!string.IsNullOrWhiteSpace(request.TrangThai) && request.TrangThai.Trim() != body.TrangThai)
        {
            await ChuyenTrangThaiAsync(maHoSoThiThe, request.TrangThai.Trim(), request.LyDoChuyenTrangThai, maTaiKhoan);
            await db.Entry(body).ReloadAsync();
        }

        // 2. Áp dụng thay đổi thông tin (các trường null = giữ nguyên)
        bool hasFieldChange = false;
        if (request.ThoiGianPhatHien.HasValue && body.ThoiGianPhatHien != request.ThoiGianPhatHien.Value)
        {
            body.ThoiGianPhatHien = request.ThoiGianPhatHien.Value;
            hasFieldChange = true;
        }
        if (request.DiaDiem != null && body.DiaDiem != request.DiaDiem)
        {
            body.DiaDiem = request.DiaDiem;
            hasFieldChange = true;
        }
        if (request.GioiTinh != null && body.GioiTinh != request.GioiTinh)
        {
            body.GioiTinh = request.GioiTinh;
            hasFieldChange = true;
        }
        if (request.TuoiUocLuong.HasValue && body.TuoiUocLuong != request.TuoiUocLuong)
        {
            body.TuoiUocLuong = request.TuoiUocLuong;
            hasFieldChange = true;
        }
        if (request.ChieuCao.HasValue && body.ChieuCao != request.ChieuCao)
        {
            body.ChieuCao = request.ChieuCao;
            hasFieldChange = true;
        }
        if (request.DacDiemCoThe != null && body.DacDiemCoThe != request.DacDiemCoThe)
        {
            body.DacDiemCoThe = request.DacDiemCoThe;
            hasFieldChange = true;
        }

        if (hasFieldChange)
        {
            await db.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public async Task ChuyenTrangThaiAsync(long maHoSoThiThe, string trangThaiMoi, string? lyDo, long maTaiKhoan)
    {
        var body = await db.Set<Body>().FindAsync(maHoSoThiThe)
                   ?? throw new KeyNotFoundException($"Không tìm thấy hồ sơ thi thể #{maHoSoThiThe}.");

        var trangThaiCu = body.TrangThai;
        var dich = trangThaiMoi.Trim();

        if (!Body.IsValidStatus(dich))
            throw new ArgumentException($"Trạng thái '{dich}' không hợp lệ.");

        if (trangThaiCu == dich)
            throw new InvalidOperationException($"Hồ sơ #{maHoSoThiThe} đang ở trạng thái '{Body.GetStatusLabel(dich)}'.");

        if (!Body.CanTransition(trangThaiCu, dich))
        {
            var allowed = Body.GetAllowedNextStatuses(trangThaiCu);
            var allowedText = allowed.Count > 0
                ? string.Join(", ", allowed.Select(s => $"{Body.GetStatusLabel(s)} ({s})"))
                : "Không có trạng thái kế tiếp";
            throw new InvalidOperationException(
                $"Không cho phép chuyển trạng thái hồ sơ từ '{Body.GetStatusLabel(trangThaiCu)}' ({trangThaiCu}) sang '{Body.GetStatusLabel(dich)}' ({dich}). Trạng thái kế tiếp hợp lệ: {allowedText}.");
        }

        var ghiChu = string.IsNullOrWhiteSpace(lyDo) ? null : lyDo.Trim();

        // Sử dụng Stored Procedure chuyên biệt cho Đóng hồ sơ và Mở lại hồ sơ
        if (trangThaiCu == Body.StatusDaBanGiao && dich == Body.StatusDongHoSo)
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"EXEC sp_DongHoSoThiThe @MaHoSoThiThe = {maHoSoThiThe}, @MaTaiKhoan = {maTaiKhoan}");

            if (!string.IsNullOrEmpty(ghiChu))
            {
                var doiTuong = Cut255($"HoSoThiThe#{maHoSoThiThe} {trangThaiCu} -> {dich}: {ghiChu}");
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"EXEC sp_GhiNhatKy @HanhDong = {NhatKyAction(dich)}, @DoiTuongTacDong = {doiTuong}, @MaTaiKhoan = {maTaiKhoan}");
            }
            return;
        }

        if (trangThaiCu == Body.StatusDongHoSo && dich == Body.StatusTiepTucLuuGiu)
        {
            if (string.IsNullOrWhiteSpace(ghiChu))
                throw new ArgumentException("Mở lại hồ sơ đã đóng bắt buộc phải nhập lý do.");

            await db.Database.ExecuteSqlInterpolatedAsync(
                $"EXEC sp_MoLaiHoSoThiThe @MaHoSoThiThe = {maHoSoThiThe}, @MaTaiKhoan = {maTaiKhoan}, @LyDo = {ghiChu}");
            return;
        }

        // Các chuyển đổi trạng thái hợp lệ khác: cập nhật trên cùng kết nối để giữ session_context cho trigger trg_Audit_HoSoThiThe
        var conn = db.Database.GetDbConnection();
        var wasOpen = conn.State == System.Data.ConnectionState.Open;
        if (!wasOpen) await conn.OpenAsync();
        try
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"EXEC sys.sp_set_session_context @key = N'user_id', @value = {maTaiKhoan}");

            body.TrangThai = dich;
            await db.SaveChangesAsync();

            // Nếu người dùng có nhập lý do/ghi chú chuyển trạng thái thì lưu thêm dòng nhật ký chi tiết
            if (!string.IsNullOrEmpty(ghiChu))
            {
                var doiTuong = Cut255($"HoSoThiThe#{maHoSoThiThe} {trangThaiCu} -> {dich}: {ghiChu}");
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"EXEC sp_GhiNhatKy @HanhDong = {NhatKyAction(dich)}, @DoiTuongTacDong = {doiTuong}, @MaTaiKhoan = {maTaiKhoan}");
            }
        }
        finally
        {
            if (!wasOpen && conn.State == System.Data.ConnectionState.Open)
                await conn.CloseAsync();
        }
    }

    /// <inheritdoc />
    public async Task<List<BodyStatusHistoryDto>> LayLichSuTrangThaiAsync(long maHoSoThiThe)
    {
        var prefix1 = $"HoSoThiThe#{maHoSoThiThe}";
        var prefix2 = $"HoSoThiThe#{maHoSoThiThe} ";
        var prefix3 = $"HoSoThiThe#{maHoSoThiThe}:";

        var logs = await db.Set<AuditLog>()
            .AsNoTracking()
            .Include(x => x.TaiKhoan)
            .Where(x => x.DoiTuongTacDong != null &&
                        (x.DoiTuongTacDong == prefix1 ||
                         x.DoiTuongTacDong.StartsWith(prefix2) ||
                         x.DoiTuongTacDong.StartsWith(prefix3)))
            .OrderByDescending(x => x.ThoiGian)
            .ThenByDescending(x => x.MaNhatKy)
            .ToListAsync();

        var result = new List<BodyStatusHistoryDto>(logs.Count);
        foreach (var log in logs)
        {
            var detail = log.DoiTuongTacDong ?? string.Empty;
            string? oldStatus = null;
            string? newStatus = null;
            string? reason = null;

            var match = StatusArrowRegex.Match(detail);
            if (match.Success)
            {
                oldStatus = match.Groups[1].Value;
                newStatus = match.Groups[2].Value;
                var colonIdx = detail.IndexOf(':', match.Index + match.Length);
                if (colonIdx >= 0 && colonIdx + 1 < detail.Length)
                    reason = detail[(colonIdx + 1)..].Trim();
            }
            else if (log.HanhDong == "MO_LAI_HO_SO")
            {
                oldStatus = Body.StatusDongHoSo;
                newStatus = Body.StatusTiepTucLuuGiu;
                var colonIdx = detail.IndexOf(':');
                if (colonIdx >= 0 && colonIdx + 1 < detail.Length)
                    reason = detail[(colonIdx + 1)..].Trim();
            }
            else if (log.HanhDong == "INSERT")
            {
                newStatus = Body.StatusMoiTiepNhan;
                reason = "Khởi tạo hồ sơ thi thể mới";
            }

            result.Add(new BodyStatusHistoryDto
            {
                MaNhatKy       = log.MaNhatKy,
                MaHoSoThiThe   = maHoSoThiThe,
                HanhDong       = log.HanhDong,
                TrangThaiCu    = oldStatus,
                TrangThaiMoi   = newStatus,
                LyDo           = reason,
                ChiTiet        = detail,
                ThoiGian       = log.ThoiGian,
                MaTaiKhoan     = log.MaTaiKhoan,
                NguoiThucHien  = log.TaiKhoan != null
                    ? $"{log.TaiKhoan.HoTen} ({log.TaiKhoan.TenDangNhap})"
                    : (log.MaTaiKhoan.HasValue ? $"Tài khoản #{log.MaTaiKhoan}" : "Hệ thống")
            });
        }

        return result;
    }

    private static string NhatKyAction(string trangThaiMoi) => trangThaiMoi switch
    {
        Body.StatusDongHoSo => "DONG_HO_SO",
        Body.StatusTiepTucLuuGiu => "MO_LAI_HO_SO",
        _ => "CHUYEN_TRANG_THAI"
    };

    private static string Cut255(string s) => s.Length <= 255 ? s : s[..255];
}
