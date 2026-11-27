using System.Data;
using DeathManagement.API.Data;
using DeathManagement.API.DTOs.MissingPerson;
using DeathManagement.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DeathManagement.API.Repositories;

/// <summary>
/// Repository truy vấn dữ liệu hồ sơ người mất tích, người thân, hình ảnh (sp_LayHinhAnhTheoHoSo)
/// và lịch sử đối sánh nghi vấn (v_DoiSanhChiTiet).
/// </summary>
public class MissingPersonRepository(ApplicationDbContext db) : IMissingPersonRepository
{
    public async Task<MissingPersonDetailDto?> GetDetailByIdAsync(
        long maHoSoMatTich,
        long maTaiKhoan,
        bool coQuyenXemHinhAnh,
        bool coQuyenXemDoiSanh)
    {
        var person = await db.MissingPersons
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.MaHoSoMatTich == maHoSoMatTich);

        if (person == null)
            return null;

        var detail = new MissingPersonDetailDto
        {
            MaHoSoMatTich    = person.MaHoSoMatTich,
            HoTen            = person.HoTen,
            NgaySinh         = person.NgaySinh,
            GioiTinh         = person.GioiTinh,
            ThoiGianMatTich  = person.ThoiGianMatTich,
            NoiMatTich       = person.NoiMatTich,
            TrangThai        = person.TrangThai,
            CoQuyenXemHinhAnh = coQuyenXemHinhAnh,
            CoQuyenXemDoiSanh = coQuyenXemDoiSanh
        };

        // 1. Danh sách tất cả người thân liên hệ (NguoiThan)
        detail.NguoiThan = await db.Relatives
            .AsNoTracking()
            .Where(x => x.MaHoSoMatTich == maHoSoMatTich)
            .OrderBy(x => x.MaNguoiThan)
            .Select(x => new MissingPersonRelativeDto
            {
                MaNguoiThan   = x.MaNguoiThan,
                MaHoSoMatTich = x.MaHoSoMatTich,
                HoTen         = x.HoTen,
                QuanHe        = x.QuanHe,
                SoDienThoai   = x.SoDienThoai,
                DiaChi        = x.DiaChi
            })
            .ToListAsync();

        // 2. Danh sách hình ảnh đã upload gọi qua sp_LayHinhAnhTheoHoSo (@MaHoSoMatTich = id)
        if (coQuyenXemHinhAnh)
        {
            detail.HinhAnh = await LayHinhAnhQuaSpAsync(maTaiKhoan, maHoSoMatTich);
        }

        // 3. Danh sách các ca đối sánh nghi vấn phát sinh với thi thể (view v_DoiSanhChiTiet)
        if (coQuyenXemDoiSanh)
        {
            detail.KetQuaDoiSanh = await db.Database.SqlQuery<MissingPersonMatchDto>($"""
                SELECT MaDoiSanh, MaHoSoThiThe, MaHoSoMatTich, HoTenNguoiMatTich,
                       DiemDoiSanh, MucDoPhuHop, TrangThai, TieuChi,
                       ThoiGianYeuCau, NguoiYeuCau
                FROM dbo.v_DoiSanhChiTiet
                WHERE MaHoSoMatTich = {maHoSoMatTich}
                ORDER BY DiemDoiSanh DESC, ThoiGianYeuCau DESC
                """).ToListAsync();
        }

        return detail;
    }

    private async Task<List<MissingPersonImageDto>> LayHinhAnhQuaSpAsync(long maTaiKhoan, long maHoSoMatTich)
    {
        var items = new List<MissingPersonImageDto>();
        var conn = db.Database.GetDbConnection();
        var shouldClose = conn.State != ConnectionState.Open;
        if (shouldClose) await conn.OpenAsync();

        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
            cmd.CommandText = "dbo.sp_LayHinhAnhTheoHoSo";
            cmd.CommandType = CommandType.StoredProcedure;

            var pAcc = cmd.CreateParameter();
            pAcc.ParameterName = "@MaTaiKhoanTruyCap";
            pAcc.Value = maTaiKhoan;
            cmd.Parameters.Add(pAcc);

            var pMissing = cmd.CreateParameter();
            pMissing.ParameterName = "@MaHoSoMatTich";
            pMissing.Value = maHoSoMatTich;
            cmd.Parameters.Add(pMissing);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(new MissingPersonImageDto
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
            if (shouldClose && conn.State == ConnectionState.Open)
                await conn.CloseAsync();
        }

        return items;
    }
}
