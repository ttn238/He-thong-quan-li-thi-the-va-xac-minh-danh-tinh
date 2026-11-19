using DeathManagement.API.Data;
using DeathManagement.API.DTOs.Body;
using DeathManagement.API.Entities;
using DeathManagement.API.Repositories.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace DeathManagement.API.Repositories;

/// <summary>
/// Triển khai repository hồ sơ thi thể, gọi trực tiếp Stored Procedures SQL Server.
/// </summary>
public class BodyRepository(ApplicationDbContext db) : IBodyRepository
{
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
    public async Task<(List<BodyDto> Items, int Total)> LayDanhSachAsync(
        string? tuKhoa, string? trangThai, int trang, int soLuong)
    {
        var query = db.Set<Body>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(trangThai))
            query = query.Where(x => x.TrangThai == trangThai);

        if (!string.IsNullOrWhiteSpace(tuKhoa))
        {
            var kw = tuKhoa.Trim();
            query = query.Where(x =>
                (x.DiaDiem != null && x.DiaDiem.Contains(kw)) ||
                (x.DacDiemCoThe != null && x.DacDiemCoThe.Contains(kw)));
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.MaHoSoThiThe)
            .Skip((trang - 1) * soLuong)
            .Take(soLuong)
            .Select(x => new BodyDto
            {
                MaHoSoThiThe    = x.MaHoSoThiThe,
                ThoiGianPhatHien = x.ThoiGianPhatHien,
                DiaDiem         = x.DiaDiem,
                TrangThai       = x.TrangThai,
                GioiTinh        = x.GioiTinh,
                TuoiUocLuong    = x.TuoiUocLuong,
                ChieuCao        = x.ChieuCao,
                DacDiemCoThe    = x.DacDiemCoThe,
                MaTinBao        = x.MaTinBao
            })
            .ToListAsync();

        return (items, total);
    }

    /// <inheritdoc />
    public async Task CapNhatAsync(long maHoSoThiThe, UpdateBodyRequest request, long maTaiKhoan)
    {
        // Lấy bản ghi hiện tại
        var body = await db.Set<Body>().FindAsync(maHoSoThiThe)
                   ?? throw new KeyNotFoundException($"Không tìm thấy hồ sơ thi thể #{maHoSoThiThe}.");

        if (body.TrangThai == "DongHoSo")
            throw new InvalidOperationException("Hồ sơ đã đóng. Vui lòng mở lại hồ sơ trước khi sửa đổi.");

        // Áp dụng thay đổi (các trường null = giữ nguyên)
        if (request.ThoiGianPhatHien.HasValue)
            body.ThoiGianPhatHien = request.ThoiGianPhatHien.Value;
        if (request.DiaDiem != null)
            body.DiaDiem = request.DiaDiem;
        if (request.GioiTinh != null)
            body.GioiTinh = request.GioiTinh;
        if (request.TuoiUocLuong.HasValue)
            body.TuoiUocLuong = request.TuoiUocLuong;
        if (request.ChieuCao.HasValue)
            body.ChieuCao = request.ChieuCao;
        if (request.DacDiemCoThe != null)
            body.DacDiemCoThe = request.DacDiemCoThe;

        await db.SaveChangesAsync();
    }
}
