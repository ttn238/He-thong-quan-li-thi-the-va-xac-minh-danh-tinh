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
public class BodyUpdateTests
{
    private sealed class InMemoryBodyRepo : IBodyRepository
    {
        public Dictionary<long, Body> Store { get; } = new()
        {
            [1] = new Body
            {
                MaHoSoThiThe = 1,
                ThoiGianPhatHien = new DateTime(2026, 9, 20, 22, 30, 0),
                DiaDiem = "Cầu Nhật Tân, Hà Nội",
                TrangThai = "DangXacMinh",
                GioiTinh = "Nam",
                TuoiUocLuong = 35,
                ChieuCao = 170m,
                DacDiemCoThe = "Vết sẹo cẳng tay trái",
                MaTinBao = 10
            },
            [2] = new Body
            {
                MaHoSoThiThe = 2,
                ThoiGianPhatHien = new DateTime(2026, 9, 25, 8, 0, 0),
                DiaDiem = "Quận 1, TP.HCM",
                TrangThai = "DongHoSo",
                GioiTinh = "Nu",
                TuoiUocLuong = 40,
                ChieuCao = 160m,
                DacDiemCoThe = "Đã đóng hồ sơ"
            }
        };

        public Task<long> TaoHoSoAsync(CreateBodyRequest request, long maTaiKhoan) => throw new NotSupportedException();
        public Task<(List<BodyDto> Items, int Total)> LayDanhSachAsync(string? tuKhoa, string? trangThai, int trang, int soLuong) => throw new NotSupportedException();
        public Task<(List<BodyDto> Items, int Total)> LayDanhSachAsync(BodyFilterRequest filter, bool nhaXac) => throw new NotSupportedException();
        public Task<BodyDto?> LayChiTietDtoAsync(long maHoSoThiThe, bool nhaXac) => Task.FromResult<BodyDto?>(null);
        public Task<BodyDetailDto?> LayChiTietDayDuAsync(long maHoSoThiThe, long maTaiKhoan, bool nhaXac, bool coQuyenXemDoVat, bool coQuyenXemHinhAnh, bool coQuyenXemDoiSanh)
            => Task.FromResult(Store.TryGetValue(maHoSoThiThe, out var b)
                ? new BodyDetailDto
                {
                    MaHoSoThiThe = b.MaHoSoThiThe,
                    ThoiGianPhatHien = b.ThoiGianPhatHien,
                    DiaDiem = b.DiaDiem,
                    TrangThai = b.TrangThai,
                    GioiTinh = b.GioiTinh,
                    TuoiUocLuong = b.TuoiUocLuong,
                    ChieuCao = b.ChieuCao,
                    DacDiemCoThe = b.DacDiemCoThe,
                    MaTinBao = b.MaTinBao
                }
                : null);
        public Task ChuyenTrangThaiAsync(long maHoSoThiThe, string trangThaiMoi, string? lyDo, long maTaiKhoan) => throw new NotSupportedException();
        public Task<List<BodyStatusHistoryDto>> LayLichSuTrangThaiAsync(long maHoSoThiThe) => Task.FromResult(new List<BodyStatusHistoryDto>());

        public Task<Body?> LayTheoMaAsync(long maHoSoThiThe)
            => Task.FromResult(Store.TryGetValue(maHoSoThiThe, out var body) ? body : null);

        public Task CapNhatAsync(long maHoSoThiThe, UpdateBodyRequest request, long maTaiKhoan)
        {
            if (!Store.TryGetValue(maHoSoThiThe, out var body))
                throw new KeyNotFoundException($"Không tìm thấy hồ sơ thi thể #{maHoSoThiThe}.");

            if (body.TrangThai == "DongHoSo")
                throw new InvalidOperationException("Hồ sơ đã đóng. Vui lòng mở lại hồ sơ trước khi sửa đổi.");

            if (request.ThoiGianPhatHien.HasValue) body.ThoiGianPhatHien = request.ThoiGianPhatHien.Value;
            if (request.DiaDiem != null) body.DiaDiem = request.DiaDiem;
            if (request.GioiTinh != null) body.GioiTinh = request.GioiTinh;
            if (request.TuoiUocLuong.HasValue) body.TuoiUocLuong = request.TuoiUocLuong;
            if (request.ChieuCao.HasValue) body.ChieuCao = request.ChieuCao;
            if (request.DacDiemCoThe != null) body.DacDiemCoThe = request.DacDiemCoThe;
            return Task.CompletedTask;
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
    public async Task OfficerCanUpdateValidBodyAndImmutableFieldsRemainIntact()
    {
        using var _ = WithPrincipal("CONG_AN", "BODY:VIEW:ALL", "BODY:UPDATE:ALL");
        var repo = new InMemoryBodyRepo();
        var service = new BodyService(repo);
        var controller = new BodyController(service);

        var newTime = new DateTime(2026, 10, 1, 9, 15, 0);
        var res = await controller.Update(1, new UpdateBodyRequest
        {
            ThoiGianPhatHien = newTime,
            DiaDiem = "  Cầu Chương Dương, Hà Nội  ",
            GioiTinh = "Nam",
            TuoiUocLuong = 38,
            ChieuCao = 172.5m,
            DacDiemCoThe = "  Có vết sẹo 5cm  "
        });

        Assert.IsType<OkObjectResult>(res);
        var updated = repo.Store[1];
        Assert.Equal(newTime, updated.ThoiGianPhatHien);
        Assert.Equal("Cầu Chương Dương, Hà Nội", updated.DiaDiem);
        Assert.Equal((short)38, updated.TuoiUocLuong);
        Assert.Equal(172.5m, updated.ChieuCao);
        Assert.Equal("Có vết sẹo 5cm", updated.DacDiemCoThe);
        // Các trường hạn chế không bị thay đổi
        Assert.Equal("DangXacMinh", updated.TrangThai);
        Assert.Equal(10, updated.MaTinBao);
    }

    [Fact]
    public async Task MorgueCanUpdateAllowedFieldsButCannotUpdateRestrictedFields()
    {
        using var _ = WithPrincipal("NHA_XAC", "BODY:VIEW:PARTIAL", "BODY:UPDATE:PARTIAL");
        var repo = new InMemoryBodyRepo();
        var service = new BodyService(repo);

        // Cập nhật hợp lệ trong phạm vi PARTIAL
        await service.CapNhatAsync(1, new UpdateBodyRequest
        {
            GioiTinh = "Khac",
            TuoiUocLuong = 36
        });
        Assert.Equal("Khac", repo.Store[1].GioiTinh);
        Assert.Equal((short)36, repo.Store[1].TuoiUocLuong);

        // Cố sửa trường bị hạn chế -> 403 Forbidden
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.CapNhatAsync(1, new UpdateBodyRequest { ThoiGianPhatHien = new DateTime(2026, 9, 21, 10, 0, 0) }));
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.CapNhatAsync(1, new UpdateBodyRequest { DiaDiem = "Thay đổi trái phép" }));
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.CapNhatAsync(1, new UpdateBodyRequest { ChieuCao = 180m }));
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.CapNhatAsync(1, new UpdateBodyRequest { DacDiemCoThe = "Thay đổi trái phép" }));
    }

    [Fact]
    public async Task InvalidDataIsRejected()
    {
        using var _ = WithPrincipal("CONG_AN", "BODY:UPDATE:ALL");
        var service = new BodyService(new InMemoryBodyRepo());

        await Assert.ThrowsAsync<ArgumentException>(() => service.CapNhatAsync(0, new UpdateBodyRequest { GioiTinh = "Nam" }));
        await Assert.ThrowsAsync<ArgumentException>(() => service.CapNhatAsync(1, new UpdateBodyRequest()));
        await Assert.ThrowsAsync<ArgumentException>(() => service.CapNhatAsync(1, new UpdateBodyRequest { ThoiGianPhatHien = DateTime.Now.AddDays(2) }));
        await Assert.ThrowsAsync<ArgumentException>(() => service.CapNhatAsync(1, new UpdateBodyRequest { DiaDiem = "   " }));
        await Assert.ThrowsAsync<ArgumentException>(() => service.CapNhatAsync(1, new UpdateBodyRequest { DiaDiem = new string('A', 256) }));
        await Assert.ThrowsAsync<ArgumentException>(() => service.CapNhatAsync(1, new UpdateBodyRequest { GioiTinh = "SaiGiaTri" }));
        await Assert.ThrowsAsync<ArgumentException>(() => service.CapNhatAsync(1, new UpdateBodyRequest { TuoiUocLuong = -1 }));
        await Assert.ThrowsAsync<ArgumentException>(() => service.CapNhatAsync(1, new UpdateBodyRequest { TuoiUocLuong = 131 }));
        await Assert.ThrowsAsync<ArgumentException>(() => service.CapNhatAsync(1, new UpdateBodyRequest { ChieuCao = 0 }));
        await Assert.ThrowsAsync<ArgumentException>(() => service.CapNhatAsync(1, new UpdateBodyRequest { ChieuCao = 301 }));
    }

    [Fact]
    public async Task MissingBodyThrowsNotFoundAndClosedBodyThrowsInvalidOperation()
    {
        using var _ = WithPrincipal("CONG_AN", "BODY:VIEW:ALL", "BODY:UPDATE:ALL");
        var service = new BodyService(new InMemoryBodyRepo());
        var controller = new BodyController(service);

        var notFoundRes = await controller.GetById(99999);
        Assert.IsType<NotFoundObjectResult>(notFoundRes);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.CapNhatAsync(99999, new UpdateBodyRequest { GioiTinh = "Nam" }));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CapNhatAsync(2, new UpdateBodyRequest { GioiTinh = "Nam" }));
    }

    [Fact]
    public async Task UnauthorizedAndUnauthenticatedAccountsAreRejected()
    {
        var service = new BodyService(new InMemoryBodyRepo());

        using (WithPrincipal(null))
        {
            await Assert.ThrowsAsync<UnauthenticatedException>(() =>
                service.CapNhatAsync(1, new UpdateBodyRequest { GioiTinh = "Nam" }));
        }

        // ADMIN chỉ có BODY:VIEW và BODY:DELETE, không có BODY:UPDATE
        using (WithPrincipal("ADMIN", "BODY:VIEW:ALL", "BODY:DELETE:ALL"))
        {
            await Assert.ThrowsAsync<ForbiddenException>(() =>
                service.CapNhatAsync(1, new UpdateBodyRequest { GioiTinh = "Nam" }));
        }

        // NGUOI_DAN không có quyền BODY
        using (WithPrincipal("NGUOI_DAN", "INCIDENT:VIEW:OWN"))
        {
            await Assert.ThrowsAsync<ForbiddenException>(() =>
                service.CapNhatAsync(1, new UpdateBodyRequest { GioiTinh = "Nam" }));
        }
    }

    [Fact]
    [Trait("Category", "LocalDatabase")]
    public async Task SqlServerUpdateWorksWithEnabledTriggersInsideRolledBackTransaction()
    {
        var connection = Environment.GetEnvironmentVariable("QLTT_TEST_DB");
        if (connection == null) return;

        using var _ = WithPrincipal("CONG_AN", "BODY:VIEW:ALL", "BODY:UPDATE:ALL");
        await using var db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options);

        var first = await db.Bodies.AsNoTracking().FirstOrDefaultAsync(x => x.TrangThai != "DongHoSo");
        if (first == null) return;

        await using var tx = await db.Database.BeginTransactionAsync();
        var repo = new BodyRepository(db);
        var service = new BodyService(repo);

        var expectedAge = (short)((first.TuoiUocLuong ?? 30) == 30 ? 31 : 30);
        await service.CapNhatAsync(first.MaHoSoThiThe, new UpdateBodyRequest
        {
            TuoiUocLuong = expectedAge,
            DiaDiem = first.DiaDiem ?? "Địa điểm kiểm thử"
        });

        var reloaded = await repo.LayTheoMaAsync(first.MaHoSoThiThe);
        Assert.NotNull(reloaded);
        Assert.Equal(expectedAge, reloaded!.TuoiUocLuong);

        await tx.RollbackAsync();
    }
}
