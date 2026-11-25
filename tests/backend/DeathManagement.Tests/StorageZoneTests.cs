using System.Security.Claims;
using DeathManagement.API.Controllers;
using DeathManagement.API.DTOs.Storage;
using DeathManagement.API.DTOs.User;
using DeathManagement.API.Entities;
using DeathManagement.API.Repositories.Interfaces;
using DeathManagement.API.Security;
using DeathManagement.API.Services;
using DeathManagement.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace DeathManagement.Tests;

[CollectionDefinition("Storage access", DisableParallelization = true)]
public class StorageAccessCollection { }

[Collection("Storage access")]
public class StorageZoneTests
{
    private class FakeStorageRepository : IStorageRepository
    {
        public List<StorageLocation> Locations { get; } = new();
        public Dictionary<int, int> Occupancy { get; } = new();

        public Task<PagedResult<StorageLocation>> GetPagedListAsync(StorageLocationFilterRequest filter)
        {
            var q = Locations.AsQueryable();
            if (!string.IsNullOrWhiteSpace(filter.Khu)) q = q.Where(x => x.Khu == filter.Khu);
            if (!string.IsNullOrWhiteSpace(filter.Tu)) q = q.Where(x => x.Tu == filter.Tu);
            var total = q.Count();
            return Task.FromResult(new PagedResult<StorageLocation>
            {
                Items = q.ToList(),
                TotalCount = total,
                PageIndex = 1,
                PageSize = 50
            });
        }

        public Task<List<StorageLocation>> GetAllAsync() => Task.FromResult(Locations.ToList());
        public Task<StorageLocation?> GetByIdAsync(int id) => Task.FromResult(Locations.FirstOrDefault(x => x.MaViTri == id));
        public Task<bool> ExistsAsync(string khu, string tu, string ngan, int? excludeId = null) =>
            Task.FromResult(Locations.Any(x => x.Khu == khu && x.Tu == tu && x.Ngan == ngan && (!excludeId.HasValue || x.MaViTri != excludeId.Value)));
        public Task<bool> HasStorageHistoryAsync(int id) => Task.FromResult(false);
        public Task<StorageLocation> AddAsync(StorageLocation location) { Locations.Add(location); return Task.FromResult(location); }
        public Task UpdateAsync(StorageLocation location) => Task.CompletedTask;
        public Task DeleteAsync(StorageLocation location) { Locations.Remove(location); return Task.CompletedTask; }
        public Task<List<string>> GetDistinctKhuListAsync() => Task.FromResult(Locations.Select(x => x.Khu).Distinct().OrderBy(x => x).ToList());
        public Task<List<string>> GetDistinctTuListAsync(string? khu = null)
        {
            var q = Locations.AsQueryable();
            if (!string.IsNullOrWhiteSpace(khu)) q = q.Where(x => x.Khu == khu);
            return Task.FromResult(q.Select(x => x.Tu).Distinct().OrderBy(x => x).ToList());
        }

        public Task<List<StorageZoneDto>> GetZonesSummaryAsync()
        {
            var zones = Locations
                .GroupBy(x => x.Khu)
                .OrderBy(g => g.Key)
                .Select(g =>
                {
                    var distinctTu = g.Select(x => x.Tu).Distinct().OrderBy(t => t).ToList();
                    var tongSucChua = g.Sum(x => x.SucChua);
                    var dangChua = g.Sum(x => Occupancy.TryGetValue(x.MaViTri, out var occ) ? occ : 0);
                    return new StorageZoneDto
                    {
                        Khu = g.Key,
                        TongSoTu = distinctTu.Count,
                        TongSoNgan = g.Count(),
                        TongSucChua = tongSucChua,
                        DangChua = dangChua,
                        DanhSachTu = distinctTu
                    };
                })
                .ToList();

            return Task.FromResult(zones);
        }

        public Task<List<StorageCabinetDto>> GetCabinetsByZoneAsync(string khu)
        {
            var cleanKhu = (khu ?? string.Empty).Trim();
            var cabinets = Locations
                .Where(x => x.Khu == cleanKhu)
                .GroupBy(x => x.Tu)
                .OrderBy(g => g.Key)
                .Select(g =>
                {
                    var tongSucChua = g.Sum(x => x.SucChua);
                    var dangChua = g.Sum(x => Occupancy.TryGetValue(x.MaViTri, out var occ) ? occ : 0);
                    return new StorageCabinetDto
                    {
                        Khu = cleanKhu,
                        Tu = g.Key,
                        TongSoNgan = g.Count(),
                        TongSucChua = tongSucChua,
                        DangChua = dangChua,
                        DanhSachNgan = g.OrderBy(x => x.Ngan).Select(x => new StorageLocationDto
                        {
                            MaViTri = x.MaViTri,
                            Khu = x.Khu,
                            Tu = x.Tu,
                            Ngan = x.Ngan,
                            SucChua = x.SucChua
                        }).ToList()
                    };
                })
                .ToList();

            return Task.FromResult(cabinets);
        }

        public Task<StorageZoneDto?> GetZoneSummaryAsync(string khu)
        {
            var cleanKhu = (khu ?? string.Empty).Trim();
            var zone = Locations
                .Where(x => x.Khu == cleanKhu)
                .GroupBy(x => x.Khu)
                .Select(g =>
                {
                    var distinctTu = g.Select(x => x.Tu).Distinct().OrderBy(t => t).ToList();
                    var tongSucChua = g.Sum(x => x.SucChua);
                    var dangChua = g.Sum(x => Occupancy.TryGetValue(x.MaViTri, out var occ) ? occ : 0);
                    return new StorageZoneDto
                    {
                        Khu = g.Key,
                        TongSoTu = distinctTu.Count,
                        TongSoNgan = g.Count(),
                        TongSucChua = tongSucChua,
                        DangChua = dangChua,
                        DanhSachTu = distinctTu
                    };
                })
                .FirstOrDefault();

            return Task.FromResult(zone);
        }
    }

    private static void SetUser(string role, string perm)
    {
        AccessControl.PrincipalProvider = () => new ClaimsPrincipal(new ClaimsIdentity(
            new[]
            {
                new Claim("sub", "1"),
                new Claim("role", role),
                new Claim("perm", perm)
            }, "test"));
    }

    // ──────────────────────────────────────────
    // 1. DTO Unit Tests (Capacity & 100% Alert)
    // ──────────────────────────────────────────

    [Fact]
    public void StorageZoneDto_WhenFull_SetsDaDayAndRedAlert()
    {
        var zone = new StorageZoneDto
        {
            Khu = "Khu A",
            TongSucChua = 10,
            DangChua = 10,
            TongSoTu = 2,
            TongSoNgan = 10
        };

        Assert.True(zone.DaDay);
        Assert.Equal(0, zone.ConTrong);
        Assert.Equal(100.0, zone.TiLeLapDay);
        Assert.Equal("DaDay", zone.MucDoCanhBao);
        Assert.Equal("#c0392b", zone.MauCanhBao); // Red
        Assert.Contains("100%", zone.CanhBao);
        Assert.Contains("Không còn ngăn trống", zone.CanhBao);
    }

    [Fact]
    public void StorageZoneDto_WhenAvailable_SetsNormalGreen()
    {
        var zone = new StorageZoneDto
        {
            Khu = "Khu B",
            TongSucChua = 10,
            DangChua = 4,
            TongSoTu = 2,
            TongSoNgan = 10
        };

        Assert.False(zone.DaDay);
        Assert.Equal(6, zone.ConTrong);
        Assert.Equal(40.0, zone.TiLeLapDay);
        Assert.Equal("BinhThuong", zone.MucDoCanhBao);
        Assert.Equal("#1fa971", zone.MauCanhBao); // Green
        Assert.Contains("còn 6 chỗ trống", zone.CanhBao);
    }

    [Fact]
    public void StorageZoneDto_WhenEightyPercent_SetsWarningAmber()
    {
        var zone = new StorageZoneDto
        {
            Khu = "Khu C",
            TongSucChua = 10,
            DangChua = 8,
            TongSoTu = 2,
            TongSoNgan = 10
        };

        Assert.False(zone.DaDay);
        Assert.Equal(2, zone.ConTrong);
        Assert.Equal(80.0, zone.TiLeLapDay);
        Assert.Equal("CanhBao", zone.MucDoCanhBao);
        Assert.Equal("#d97706", zone.MauCanhBao); // Amber
    }

    [Fact]
    public void StorageCabinetDto_CalculatesCapacityAndOccupancy()
    {
        var cabinet = new StorageCabinetDto
        {
            Khu = "Khu A",
            Tu = "Tủ 01",
            TongSucChua = 5,
            DangChua = 5,
            TongSoNgan = 5
        };

        Assert.True(cabinet.DaDay);
        Assert.Equal(0, cabinet.ConTrong);
        Assert.Equal(100.0, cabinet.TiLeLapDay);
        Assert.Equal("DaDay", cabinet.MucDoCanhBao);
        Assert.Equal("#c0392b", cabinet.MauCanhBao);
    }

    // ──────────────────────────────────────────
    // 2. Service & Repository Tests
    // ──────────────────────────────────────────

    [Fact]
    public async Task StorageService_GetZonesAsync_ReturnsGroupedZonesWithStatistics()
    {
        var previous = AccessControl.PrincipalProvider;
        SetUser("ADMIN", "STORAGE:VIEW:ALL");
        try
        {
            var repo = new FakeStorageRepository();
            repo.Locations.Add(new StorageLocation { MaViTri = 1, Khu = "Khu A", Tu = "Tủ 1", Ngan = "N01", SucChua = 1 });
            repo.Locations.Add(new StorageLocation { MaViTri = 2, Khu = "Khu A", Tu = "Tủ 1", Ngan = "N02", SucChua = 1 });
            repo.Locations.Add(new StorageLocation { MaViTri = 3, Khu = "Khu A", Tu = "Tủ 2", Ngan = "N01", SucChua = 1 });
            repo.Locations.Add(new StorageLocation { MaViTri = 4, Khu = "Khu B", Tu = "Tủ 1", Ngan = "N01", SucChua = 2 });

            // Set Khu A as 100% full (3/3 occupied)
            repo.Occupancy[1] = 1;
            repo.Occupancy[2] = 1;
            repo.Occupancy[3] = 1;

            // Set Khu B as partially occupied (1/2)
            repo.Occupancy[4] = 1;

            var service = new StorageService(repo);
            var zones = await service.GetZonesAsync();

            Assert.Equal(2, zones.Count);

            var khuA = zones.First(x => x.Khu == "Khu A");
            Assert.Equal(2, khuA.TongSoTu);
            Assert.Equal(3, khuA.TongSoNgan);
            Assert.Equal(3, khuA.TongSucChua);
            Assert.Equal(3, khuA.DangChua);
            Assert.True(khuA.DaDay); // Alert 100% full
            Assert.Equal("#c0392b", khuA.MauCanhBao);

            var khuB = zones.First(x => x.Khu == "Khu B");
            Assert.Equal(1, khuB.TongSoTu);
            Assert.Equal(1, khuB.TongSoNgan);
            Assert.Equal(2, khuB.TongSucChua);
            Assert.Equal(1, khuB.DangChua);
            Assert.False(khuB.DaDay);
            Assert.Equal(1, khuB.ConTrong);
        }
        finally
        {
            AccessControl.PrincipalProvider = previous;
        }
    }

    [Fact]
    public async Task StorageService_GetCabinetsByZoneAsync_ReturnsCabinetsForZone()
    {
        var previous = AccessControl.PrincipalProvider;
        SetUser("ADMIN", "STORAGE:VIEW:ALL");
        try
        {
            var repo = new FakeStorageRepository();
            repo.Locations.Add(new StorageLocation { MaViTri = 1, Khu = "Khu A", Tu = "Tủ 1", Ngan = "N01", SucChua = 1 });
            repo.Locations.Add(new StorageLocation { MaViTri = 2, Khu = "Khu A", Tu = "Tủ 1", Ngan = "N02", SucChua = 1 });
            repo.Locations.Add(new StorageLocation { MaViTri = 3, Khu = "Khu A", Tu = "Tủ 2", Ngan = "N01", SucChua = 1 });

            var service = new StorageService(repo);
            var cabinets = await service.GetCabinetsByZoneAsync("Khu A");

            Assert.Equal(2, cabinets.Count);
            Assert.Equal("Tủ 1", cabinets[0].Tu);
            Assert.Equal(2, cabinets[0].TongSoNgan);
            Assert.Equal(2, cabinets[0].TongSucChua);
            Assert.Equal("Tủ 2", cabinets[1].Tu);
            Assert.Equal(1, cabinets[1].TongSoNgan);

            // Empty zone name should throw ArgumentException
            await Assert.ThrowsAsync<ArgumentException>(() => service.GetCabinetsByZoneAsync(""));
        }
        finally
        {
            AccessControl.PrincipalProvider = previous;
        }
    }

    [Fact]
    public async Task StorageService_WithoutPermission_ThrowsForbiddenException()
    {
        var previous = AccessControl.PrincipalProvider;
        // User with no STORAGE permissions
        SetUser("USER", "BODY:VIEW:ALL");
        try
        {
            var repo = new FakeStorageRepository();
            var service = new StorageService(repo);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetZonesAsync());
            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetCabinetsByZoneAsync("Khu A"));
        }
        finally
        {
            AccessControl.PrincipalProvider = previous;
        }
    }

    // ──────────────────────────────────────────
    // 3. Controller Endpoint Tests
    // ──────────────────────────────────────────

    [Fact]
    public async Task StorageController_GetZones_ReturnsOkWithZoneList()
    {
        var previous = AccessControl.PrincipalProvider;
        SetUser("ADMIN", "STORAGE:VIEW:ALL");
        try
        {
            var repo = new FakeStorageRepository();
            repo.Locations.Add(new StorageLocation { MaViTri = 1, Khu = "Khu A", Tu = "Tủ 1", Ngan = "N01", SucChua = 1 });
            var service = new StorageService(repo);
            var controller = new StorageController(service);

            var actionResult = await controller.GetZones();
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var zones = Assert.IsAssignableFrom<List<StorageZoneDto>>(okResult.Value);

            Assert.Single(zones);
            Assert.Equal("Khu A", zones[0].Khu);
        }
        finally
        {
            AccessControl.PrincipalProvider = previous;
        }
    }

    [Fact]
    public async Task StorageController_GetCabinetsByZone_ReturnsOkOrBadRequest()
    {
        var previous = AccessControl.PrincipalProvider;
        SetUser("ADMIN", "STORAGE:VIEW:ALL");
        try
        {
            var repo = new FakeStorageRepository();
            repo.Locations.Add(new StorageLocation { MaViTri = 1, Khu = "Khu A", Tu = "Tủ 1", Ngan = "N01", SucChua = 1 });
            var service = new StorageService(repo);
            var controller = new StorageController(service);

            // Valid zone
            var result = await controller.GetCabinetsByZone("Khu A");
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var cabinets = Assert.IsAssignableFrom<List<StorageCabinetDto>>(okResult.Value);
            Assert.Single(cabinets);

            // Empty zone -> BadRequest
            var badResult = await controller.GetCabinetsByZone("");
            Assert.IsType<BadRequestObjectResult>(badResult.Result);
        }
        finally
        {
            AccessControl.PrincipalProvider = previous;
        }
    }
}
