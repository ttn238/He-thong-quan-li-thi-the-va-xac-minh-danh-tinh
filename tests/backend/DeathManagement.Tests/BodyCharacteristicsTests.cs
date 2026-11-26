using System.Security.Claims;
using DeathManagement.API.Controllers;
using DeathManagement.API.Data;
using DeathManagement.API.DTOs.Body;
using DeathManagement.API.Entities;
using DeathManagement.API.Repositories;
using DeathManagement.API.Repositories.Interfaces;
using DeathManagement.API.Security;
using DeathManagement.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeathManagement.Tests;

[Collection("Body access")]
public class BodyCharacteristicsTests
{
    private sealed class InMemoryCharacteristicsRepo : IBodyRepository
    {
        private long _nextId = 10;
        public Dictionary<long, Body> Bodies { get; } = new()
        {
            [1] = new Body
            {
                MaHoSoThiThe = 1,
                ThoiGianPhatHien = new DateTime(2026, 9, 20, 22, 30, 0),
                DiaDiem = "Cầu Nhật Tân, Hà Nội",
                TrangThai = Body.StatusDangXacMinh
            },
            [2] = new Body
            {
                MaHoSoThiThe = 2,
                ThoiGianPhatHien = new DateTime(2026, 9, 21, 8, 0, 0),
                DiaDiem = "Quận 1, TP.HCM",
                TrangThai = Body.StatusDongHoSo
            }
        };

        public List<BodyCharacteristicDto> Items { get; } =
        [
            new BodyCharacteristicDto
            {
                MaDacDiem = 1,
                MaHoSoThiThe = 1,
                LoaiDacDiem = BodyCharacteristic.TypeSeo,
                ViTriCoThe = "Cẳng tay trái",
                MoTa = "Vết sẹo dài 5cm",
                NgayGhiNhan = new DateTime(2026, 9, 20, 23, 0, 0)
            }
        ];

        public Task<long> TaoHoSoAsync(CreateBodyRequest request, long maTaiKhoan) => throw new NotSupportedException();
        public Task<Body?> LayTheoMaAsync(long maHoSoThiThe) => Task.FromResult(Bodies.TryGetValue(maHoSoThiThe, out var b) ? b : null);
        public Task<BodyDto?> LayChiTietDtoAsync(long maHoSoThiThe, bool nhaXac) => Task.FromResult<BodyDto?>(null);
        public Task<BodyDetailDto?> LayChiTietDayDuAsync(long maHoSoThiThe, long maTaiKhoan, bool nhaXac, bool coQuyenXemDoVat, bool coQuyenXemHinhAnh, bool coQuyenXemDoiSanh)
            => Task.FromResult<BodyDetailDto?>(null);
        public Task<(List<BodyDto> Items, int Total)> LayDanhSachAsync(string? tuKhoa, string? trangThai, int trang, int soLuong) => throw new NotSupportedException();
        public Task<(List<BodyDto> Items, int Total)> LayDanhSachAsync(BodyFilterRequest filter, bool nhaXac) => throw new NotSupportedException();
        public Task CapNhatAsync(long maHoSoThiThe, UpdateBodyRequest request, long maTaiKhoan) => throw new NotSupportedException();
        public Task ChuyenTrangThaiAsync(long maHoSoThiThe, string trangThaiMoi, string? lyDo, long maTaiKhoan) => throw new NotSupportedException();
        public Task<List<BodyStatusHistoryDto>> LayLichSuTrangThaiAsync(long maHoSoThiThe) => Task.FromResult(new List<BodyStatusHistoryDto>());

        public Task<List<BodyCharacteristicDto>> LayDacDiemNhanDangAsync(long maHoSoThiThe)
            => Task.FromResult(Items.Where(x => x.MaHoSoThiThe == maHoSoThiThe).ToList());

        public Task<BodyCharacteristicDto> ThemDacDiemNhanDangAsync(long maHoSoThiThe, SaveBodyCharacteristicRequest request, long maTaiKhoan)
        {
            if (!Bodies.TryGetValue(maHoSoThiThe, out var body))
                throw new KeyNotFoundException($"Không tìm thấy hồ sơ thi thể #{maHoSoThiThe}.");
            if (body.TrangThai == Body.StatusDongHoSo)
                throw new InvalidOperationException("Hồ sơ đã đóng.");

            var created = new BodyCharacteristicDto
            {
                MaDacDiem = ++_nextId,
                MaHoSoThiThe = maHoSoThiThe,
                LoaiDacDiem = request.LoaiDacDiem,
                ViTriCoThe = request.ViTriCoThe,
                MoTa = request.MoTa,
                NgayGhiNhan = request.NgayGhiNhan ?? DateTime.Now
            };
            Items.Add(created);
            return Task.FromResult(created);
        }

        public Task<BodyCharacteristicDto> CapNhatDacDiemNhanDangAsync(long maHoSoThiThe, long maDacDiem, SaveBodyCharacteristicRequest request, long maTaiKhoan)
        {
            if (!Bodies.TryGetValue(maHoSoThiThe, out var body))
                throw new KeyNotFoundException($"Không tìm thấy hồ sơ thi thể #{maHoSoThiThe}.");
            if (body.TrangThai == Body.StatusDongHoSo)
                throw new InvalidOperationException("Hồ sơ đã đóng.");

            var existing = Items.FirstOrDefault(x => x.MaDacDiem == maDacDiem && x.MaHoSoThiThe == maHoSoThiThe)
                ?? throw new KeyNotFoundException($"Không tìm thấy đặc điểm #{maDacDiem}.");

            existing.LoaiDacDiem = request.LoaiDacDiem;
            existing.ViTriCoThe = request.ViTriCoThe;
            existing.MoTa = request.MoTa;
            if (request.NgayGhiNhan.HasValue) existing.NgayGhiNhan = request.NgayGhiNhan.Value;
            return Task.FromResult(existing);
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
            var claims = new List<Claim> { new("sub", "2"), new("role", role) };
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
    public async Task OfficerCanViewCreateAndUpdateCharacteristics()
    {
        using var _ = WithPrincipal("CONG_AN", "BODY:VIEW:ALL", "BODY:UPDATE:ALL");
        var repo = new InMemoryCharacteristicsRepo();
        var service = new BodyService(repo);
        var controller = new BodyController(service);

        // Xem danh sách
        var listRes = Assert.IsType<OkObjectResult>(await controller.GetCharacteristics(1));
        var list = Assert.IsType<List<BodyCharacteristicDto>>(listRes.Value);
        Assert.Single(list);

        // Thêm mới
        var createRes = Assert.IsType<CreatedResult>(await controller.CreateCharacteristic(1, new SaveBodyCharacteristicRequest
        {
            LoaiDacDiem = BodyCharacteristic.TypeHinhXam,
            ViTriCoThe = "  Bả vai phải  ",
            MoTa = "  Hình xăm rồng màu đen  ",
            NgayGhiNhan = new DateTime(2026, 9, 21, 9, 0, 0)
        }));
        Assert.Equal(2, repo.Items.Count);
        var added = repo.Items.Last();
        Assert.Equal("Bả vai phải", added.ViTriCoThe);
        Assert.Equal("Hình xăm rồng màu đen", added.MoTa);

        // Cập nhật
        var updateRes = Assert.IsType<OkObjectResult>(await controller.UpdateCharacteristic(1, added.MaDacDiem, new SaveBodyCharacteristicRequest
        {
            LoaiDacDiem = BodyCharacteristic.TypeHinhXam,
            ViTriCoThe = "Bả vai phải",
            MoTa = "Hình xăm rồng dài 12cm"
        }));
        Assert.NotNull(updateRes.Value);
        Assert.Equal("Hình xăm rồng dài 12cm", repo.Items.First(x => x.MaDacDiem == added.MaDacDiem).MoTa);
    }

    [Fact]
    public async Task InvalidCharacteristicInputIsRejected()
    {
        using var _ = WithPrincipal("CONG_AN", "BODY:UPDATE:ALL");
        var service = new BodyService(new InMemoryCharacteristicsRepo());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ThemDacDiemNhanDangAsync(0, new SaveBodyCharacteristicRequest { LoaiDacDiem = "Seo", MoTa = "Test" }));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ThemDacDiemNhanDangAsync(1, new SaveBodyCharacteristicRequest { LoaiDacDiem = "SaiLoai", MoTa = "Test" }));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ThemDacDiemNhanDangAsync(1, new SaveBodyCharacteristicRequest { LoaiDacDiem = "Seo", MoTa = "   " }));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ThemDacDiemNhanDangAsync(1, new SaveBodyCharacteristicRequest { LoaiDacDiem = "Seo", MoTa = new string('A', 501) }));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ThemDacDiemNhanDangAsync(1, new SaveBodyCharacteristicRequest { LoaiDacDiem = "Seo", ViTriCoThe = new string('B', 151), MoTa = "Hợp lệ" }));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ThemDacDiemNhanDangAsync(1, new SaveBodyCharacteristicRequest { LoaiDacDiem = "Seo", MoTa = "Hợp lệ", NgayGhiNhan = DateTime.Now.AddDays(1) }));
    }

    [Fact]
    public async Task MissingBodyOrClosedBodyIsHandled()
    {
        using var _ = WithPrincipal("CONG_AN", "BODY:VIEW:ALL", "BODY:UPDATE:ALL");
        var service = new BodyService(new InMemoryCharacteristicsRepo());

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.LayDacDiemNhanDangAsync(99999));
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.ThemDacDiemNhanDangAsync(99999, new SaveBodyCharacteristicRequest { LoaiDacDiem = "Seo", MoTa = "Sẹo trán" }));
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.CapNhatDacDiemNhanDangAsync(1, 99999, new SaveBodyCharacteristicRequest { LoaiDacDiem = "Seo", MoTa = "Sẹo trán" }));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ThemDacDiemNhanDangAsync(2, new SaveBodyCharacteristicRequest { LoaiDacDiem = "Seo", MoTa = "Hồ sơ đã đóng" }));
    }

    [Fact]
    public async Task UnauthorizedAndRestrictedUsersAreRejected()
    {
        var service = new BodyService(new InMemoryCharacteristicsRepo());

        using (WithPrincipal(null))
        {
            await Assert.ThrowsAsync<UnauthenticatedException>(() => service.LayDacDiemNhanDangAsync(1));
            await Assert.ThrowsAsync<UnauthenticatedException>(() =>
                service.ThemDacDiemNhanDangAsync(1, new SaveBodyCharacteristicRequest { LoaiDacDiem = "Seo", MoTa = "Test" }));
        }

        // NHA_XAC (PARTIAL) bị chặn xem và sửa đặc điểm nhận dạng nhạy cảm
        using (WithPrincipal("NHA_XAC", "BODY:VIEW:PARTIAL", "BODY:UPDATE:PARTIAL"))
        {
            await Assert.ThrowsAsync<ForbiddenException>(() => service.LayDacDiemNhanDangAsync(1));
            await Assert.ThrowsAsync<ForbiddenException>(() =>
                service.ThemDacDiemNhanDangAsync(1, new SaveBodyCharacteristicRequest { LoaiDacDiem = "Seo", MoTa = "Test" }));
        }

        // ADMIN chỉ có BODY:VIEW:ALL (được xem, không được thêm/sửa)
        using (WithPrincipal("ADMIN", "BODY:VIEW:ALL", "BODY:DELETE:ALL"))
        {
            var items = await service.LayDacDiemNhanDangAsync(1);
            Assert.Single(items);
            await Assert.ThrowsAsync<ForbiddenException>(() =>
                service.ThemDacDiemNhanDangAsync(1, new SaveBodyCharacteristicRequest { LoaiDacDiem = "Seo", MoTa = "Test" }));
        }

        // NGUOI_DAN không có quyền BODY
        using (WithPrincipal("NGUOI_DAN", "INCIDENT:VIEW:OWN"))
        {
            await Assert.ThrowsAsync<ForbiddenException>(() => service.LayDacDiemNhanDangAsync(1));
        }
    }

    [Fact]
    [Trait("Category", "LocalDatabase")]
    public async Task SqlServerCreateUpdateAndSyncSummaryWorksInsideRolledBackTransaction()
    {
        var connection = Environment.GetEnvironmentVariable("QLTT_TEST_DB");
        if (connection == null) return;

        using var _ = WithPrincipal("CONG_AN", "BODY:VIEW:ALL", "BODY:UPDATE:ALL");
        await using var db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options);

        var first = await db.Bodies.AsNoTracking().FirstOrDefaultAsync(x => x.TrangThai != Body.StatusDongHoSo);
        if (first == null) return;

        // Đảm bảo bảng tồn tại trước transaction để không bị ảnh hưởng DDL
        var repo = new BodyRepository(db);
        await repo.LayDacDiemNhanDangAsync(first.MaHoSoThiThe);

        await using var tx = await db.Database.BeginTransactionAsync();
        var service = new BodyService(repo);

        var added = await service.ThemDacDiemNhanDangAsync(first.MaHoSoThiThe, new SaveBodyCharacteristicRequest
        {
            LoaiDacDiem = BodyCharacteristic.TypeSeo,
            ViTriCoThe = "Cổ tay phải",
            MoTa = "Vết sẹo kiểm thử 6.7",
            NgayGhiNhan = DateTime.Now.AddHours(-1)
        });
        Assert.True(added.MaDacDiem > 0);

        var updated = await service.CapNhatDacDiemNhanDangAsync(first.MaHoSoThiThe, added.MaDacDiem, new SaveBodyCharacteristicRequest
        {
            LoaiDacDiem = BodyCharacteristic.TypeHinhXam,
            ViTriCoThe = "Cổ tay phải",
            MoTa = "Hình xăm neo biển kiểm thử 6.7"
        });
        Assert.Equal(BodyCharacteristic.TypeHinhXam, updated.LoaiDacDiem);

        var listed = await service.LayDacDiemNhanDangAsync(first.MaHoSoThiThe);
        Assert.Contains(listed, x => x.MaDacDiem == added.MaDacDiem && x.MoTa == "Hình xăm neo biển kiểm thử 6.7");

        await tx.RollbackAsync();
    }
}
