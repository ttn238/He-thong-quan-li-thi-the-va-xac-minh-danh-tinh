using System.Security.Claims;
using DeathManagement.API.Controllers;
using DeathManagement.API.Data;
using DeathManagement.API.DTOs.Storage;
using DeathManagement.API.DTOs.User;
using DeathManagement.API.Entities;
using DeathManagement.API.Repositories;
using DeathManagement.API.Repositories.Interfaces;
using DeathManagement.API.Security;
using DeathManagement.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeathManagement.Tests;

[Collection("Body access")]
public class StorageMovementHistoryTests
{
    private sealed class FakeStorageRepo : IStorageRepository
    {
        public Dictionary<long, List<StorageMovementHistoryDto>> History { get; } = new()
        {
            [1] =
            [
                new StorageMovementHistoryDto
                {
                    MaLichSu = 10,
                    MaHoSoThiThe = 1,
                    MaViTri = 1,
                    Khu = "A",
                    Tu = "A01",
                    Ngan = "N01",
                    ThoiGianVao = new DateTime(2026, 9, 20, 23, 0, 0),
                    ThoiGianRa = new DateTime(2026, 9, 21, 9, 30, 0),
                    NguoiThaoTac = "Lê Văn Cường (nhaxac01)",
                    LyDo = "Tiếp nhận và xếp ngăn lưu trữ ban đầu",
                    LaViTriHienTai = false
                },
                new StorageMovementHistoryDto
                {
                    MaLichSu = 11,
                    MaHoSoThiThe = 1,
                    MaViTri = 2,
                    Khu = "A",
                    Tu = "A01",
                    Ngan = "N02",
                    ThoiGianVao = new DateTime(2026, 9, 21, 9, 30, 0),
                    ThoiGianRa = null,
                    NguoiThaoTac = "Nguyễn Văn An (canbo01)",
                    LyDo = "Điều chuyển bảo trì ngăn N01",
                    LaViTriHienTai = true
                }
            ]
        };

        public Task<PagedResult<StorageLocation>> GetPagedListAsync(StorageLocationFilterRequest filter) => throw new NotSupportedException();
        public Task<List<StorageLocation>> GetAllAsync() => throw new NotSupportedException();
        public Task<StorageLocation?> GetByIdAsync(int id) => throw new NotSupportedException();
        public Task<bool> ExistsAsync(string khu, string tu, string ngan, int? excludeId = null) => throw new NotSupportedException();
        public Task<bool> HasStorageHistoryAsync(int id) => throw new NotSupportedException();
        public Task<StorageLocation> AddAsync(StorageLocation location) => throw new NotSupportedException();
        public Task UpdateAsync(StorageLocation location) => throw new NotSupportedException();
        public Task DeleteAsync(StorageLocation location) => throw new NotSupportedException();
        public Task<List<string>> GetDistinctKhuListAsync() => throw new NotSupportedException();
        public Task<List<string>> GetDistinctTuListAsync(string? khu = null) => throw new NotSupportedException();
        public Task<List<StorageZoneDto>> GetZonesSummaryAsync() => throw new NotSupportedException();
        public Task<List<StorageCabinetDto>> GetCabinetsByZoneAsync(string khu) => throw new NotSupportedException();
        public Task<StorageZoneDto?> GetZoneSummaryAsync(string khu) => throw new NotSupportedException();

        public Task<List<StorageMovementHistoryDto>> GetMovementHistoryAsync(long maHoSoThiThe)
        {
            if (!History.TryGetValue(maHoSoThiThe, out var list))
                throw new KeyNotFoundException($"Không tìm thấy hồ sơ thi thể #{maHoSoThiThe}.");
            return Task.FromResult(list);
        }
    }

    private static IDisposable WithPrincipal(string? role, params string[] perms)
    {
        var previous = AccessControl.PrincipalProvider;
        if (role == null)
        {
            AccessControl.PrincipalProvider = () => null;
        }
        else
        {
            var claims = new List<Claim> { new("sub", "4"), new("role", role) };
            claims.AddRange(perms.Select(p => new Claim("perm", p)));
            AccessControl.PrincipalProvider = () => new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        }
        return new RestoreScope(previous);
    }

    private sealed class RestoreScope(Func<ClaimsPrincipal?> previous) : IDisposable
    {
        public void Dispose() => AccessControl.PrincipalProvider = previous;
    }

    [Fact]
    public async Task AuthorizedUserGetsChronologicalTimelineWithActiveBadge()
    {
        using var _ = WithPrincipal("NHA_XAC", "STORAGE:VIEW:ALL");
        var service = new StorageService(new FakeStorageRepo());
        var controller = new StorageController(service);

        var actionResult = await controller.GetMovementHistory(1);
        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        var timeline = Assert.IsType<List<StorageMovementHistoryDto>>(ok.Value);

        Assert.Equal(2, timeline.Count);
        Assert.Equal("A - A01 - N01", timeline[0].ViTriDayDu);
        Assert.NotNull(timeline[0].ThoiGianRa);
        Assert.False(timeline[0].LaViTriHienTai);

        Assert.Equal("A - A01 - N02", timeline[1].ViTriDayDu);
        Assert.Null(timeline[1].ThoiGianRa);
        Assert.True(timeline[1].LaViTriHienTai);
        Assert.Equal("Điều chuyển bảo trì ngăn N01", timeline[1].LyDo);
    }

    [Fact]
    public async Task InvalidIdAndMissingBodyReturnBadRequestAndNotFound()
    {
        using var _ = WithPrincipal("CONG_AN", "BODY:VIEW:ALL");
        var controller = new StorageController(new StorageService(new FakeStorageRepo()));

        var bad = await controller.GetMovementHistory(0);
        Assert.IsType<BadRequestObjectResult>(bad.Result);

        var notFound = await controller.GetMovementHistory(99999);
        Assert.IsType<NotFoundObjectResult>(notFound.Result);
    }

    [Fact]
    public async Task UnauthenticatedAndUnauthorizedUsersAreRejected()
    {
        var service = new StorageService(new FakeStorageRepo());

        using (WithPrincipal(null))
        {
            await Assert.ThrowsAsync<UnauthenticatedException>(() => service.GetMovementHistoryAsync(1));
        }

        using (WithPrincipal("NGUOI_DAN", "INCIDENT:VIEW:OWN"))
        {
            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetMovementHistoryAsync(1));
        }
    }

    [Fact]
    [Trait("Category", "LocalDatabase")]
    public async Task SqlServerMovementHistoryComputesExitTimeAndActiveFlag()
    {
        var connection = Environment.GetEnvironmentVariable("QLTT_TEST_DB");
        if (connection == null) return;

        using var _ = WithPrincipal("NHA_XAC", "STORAGE:VIEW:ALL");
        await using var db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options);

        var body = await db.Bodies.AsNoTracking()
            .FirstOrDefaultAsync(x => x.TrangThai != Body.StatusDaBanGiao && x.TrangThai != Body.StatusDongHoSo);
        var locations = await db.StorageLocations.AsNoTracking().OrderBy(x => x.MaViTri).Take(2).ToListAsync();
        if (body == null || locations.Count < 2) return;

        await using var tx = await db.Database.BeginTransactionAsync();

        var t1 = new DateTime(2026, 10, 1, 8, 0, 0);
        var t2 = new DateTime(2026, 10, 2, 14, 30, 0);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO dbo.LichSuLuuTru (ThoiGian, MaHoSoThiThe, MaViTri) VALUES ({t1}, {body.MaHoSoThiThe}, {locations[0].MaViTri})");
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO dbo.LichSuLuuTru (ThoiGian, MaHoSoThiThe, MaViTri) VALUES ({t2}, {body.MaHoSoThiThe}, {locations[1].MaViTri})");

        var repo = new StorageRepository(db);
        var timeline = await repo.GetMovementHistoryAsync(body.MaHoSoThiThe);

        Assert.True(timeline.Count >= 2);
        var secondLast = timeline[^2];
        var last = timeline[^1];
        Assert.Equal(t1, secondLast.ThoiGianVao);
        Assert.Equal(t2, secondLast.ThoiGianRa);
        Assert.False(secondLast.LaViTriHienTai);

        Assert.Equal(t2, last.ThoiGianVao);
        Assert.Null(last.ThoiGianRa);
        Assert.True(last.LaViTriHienTai);
        Assert.Equal($"{locations[1].Khu} - {locations[1].Tu} - {locations[1].Ngan}", last.ViTriDayDu);

        await tx.RollbackAsync();
    }
}
