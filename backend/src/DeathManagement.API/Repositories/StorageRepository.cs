using System.Data;
using DeathManagement.API.Data;
using DeathManagement.API.DTOs.Storage;
using DeathManagement.API.DTOs.User;
using DeathManagement.API.Entities;
using DeathManagement.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DeathManagement.API.Repositories;

/// <summary>
/// Kho lưu trữ dữ liệu vị trí lưu trữ thi thể tương tác trực tiếp với bảng ViTriLuuTru
/// </summary>
public class StorageRepository : IStorageRepository
{
    private readonly ApplicationDbContext? _injectedDb;

    public StorageRepository(ApplicationDbContext? db = null)
    {
        _injectedDb = db;
    }

    private ApplicationDbContext CreateDb() => _injectedDb ?? Program.NewDb();

    public async Task<PagedResult<StorageLocation>> GetPagedListAsync(StorageLocationFilterRequest filter)
    {
        using var db = CreateDb();
        var query = db.StorageLocations.AsNoTracking().AsQueryable();

        // 1. Tìm kiếm theo từ khóa (Khu, Tủ, Ngăn, Vị trí đầy đủ)
        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var kw = filter.Keyword.Trim();
            query = query.Where(x =>
                x.Khu.Contains(kw) ||
                x.Tu.Contains(kw) ||
                x.Ngan.Contains(kw) ||
                (x.Khu + " - " + x.Tu + " - " + x.Ngan).Contains(kw));
        }

        // 2. Lọc theo Khu
        if (!string.IsNullOrWhiteSpace(filter.Khu))
        {
            var khu = filter.Khu.Trim();
            query = query.Where(x => x.Khu == khu);
        }

        // 3. Lọc theo Tủ
        if (!string.IsNullOrWhiteSpace(filter.Tu))
        {
            var tu = filter.Tu.Trim();
            query = query.Where(x => x.Tu == tu);
        }

        var totalCount = await query.CountAsync();

        var pageIndex = filter.PageIndex > 0 ? filter.PageIndex : 1;
        var pageSize = filter.PageSize > 0 ? filter.PageSize : 10;

        var items = await query
            .OrderBy(x => x.Khu)
            .ThenBy(x => x.Tu)
            .ThenBy(x => x.Ngan)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<StorageLocation>
        {
            Items = items,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }

    public async Task<List<StorageLocation>> GetAllAsync()
    {
        using var db = CreateDb();
        return await db.StorageLocations
            .AsNoTracking()
            .OrderBy(x => x.Khu)
            .ThenBy(x => x.Tu)
            .ThenBy(x => x.Ngan)
            .ToListAsync();
    }

    public async Task<StorageLocation?> GetByIdAsync(int id)
    {
        using var db = CreateDb();
        return await db.StorageLocations
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.MaViTri == id);
    }

    public async Task<bool> ExistsAsync(string khu, string tu, string ngan, int? excludeId = null)
    {
        using var db = CreateDb();
        var cleanKhu = khu.Trim();
        var cleanTu = tu.Trim();
        var cleanNgan = ngan.Trim();

        return await db.StorageLocations.AsNoTracking().AnyAsync(x =>
            x.Khu == cleanKhu &&
            x.Tu == cleanTu &&
            x.Ngan == cleanNgan &&
            (!excludeId.HasValue || x.MaViTri != excludeId.Value));
    }

    public async Task<bool> HasStorageHistoryAsync(int id)
    {
        using var db = CreateDb();
        try
        {
            var conn = db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
            {
                await conn.OpenAsync();
            }

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(1) FROM dbo.LichSuLuuTru WHERE MaViTri = @id";
            var param = cmd.CreateParameter();
            param.ParameterName = "@id";
            param.Value = id;
            cmd.Parameters.Add(param);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }
        catch
        {
            return false;
        }
    }

    public async Task<StorageLocation> AddAsync(StorageLocation location)
    {
        using var db = CreateDb();
        db.StorageLocations.Add(location);
        await db.SaveChangesAsync();
        return location;
    }

    public async Task UpdateAsync(StorageLocation location)
    {
        using var db = CreateDb();
        db.StorageLocations.Update(location);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(StorageLocation location)
    {
        using var db = CreateDb();
        db.StorageLocations.Remove(location);
        await db.SaveChangesAsync();
    }

    public async Task<List<string>> GetDistinctKhuListAsync()
    {
        using var db = CreateDb();
        return await db.StorageLocations
            .AsNoTracking()
            .Select(x => x.Khu)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();
    }

    public async Task<List<string>> GetDistinctTuListAsync(string? khu = null)
    {
        using var db = CreateDb();
        var query = db.StorageLocations.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(khu))
        {
            var cleanKhu = khu.Trim();
            query = query.Where(x => x.Khu == cleanKhu);
        }

        return await query
            .Select(x => x.Tu)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();
    }
}
