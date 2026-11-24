using System.Security.Claims;
using DeathManagement.API.Data;
using DeathManagement.API.DTOs.Body;
using DeathManagement.API.Entities;
using DeathManagement.API.Repositories;
using DeathManagement.API.Repositories.Interfaces;
using DeathManagement.API.Security;
using DeathManagement.API.Services;
using Microsoft.EntityFrameworkCore;

namespace DeathManagement.Tests;

[CollectionDefinition("Body access", DisableParallelization = true)]
public class BodyAccessCollection { }

[Collection("Body access")]
public class BodyListTests
{
    private class Repository : IBodyRepository
    {
        public bool Restricted;
        public Task<(List<BodyDto> Items, int Total)> LayDanhSachAsync(BodyFilterRequest f, bool nhaXac)
        { Restricted = nhaXac; return Task.FromResult((new List<BodyDto>(), 0)); }
        public Task<(List<BodyDto> Items, int Total)> LayDanhSachAsync(string? k, string? s, int p, int n) => throw new NotSupportedException();
        public Task<Body?> LayTheoMaAsync(long id) => Task.FromResult<Body?>(new Body { MaHoSoThiThe = id, DiaDiem = "secret", DacDiemCoThe = "secret", ChieuCao = 170 });
        public Task<long> TaoHoSoAsync(CreateBodyRequest r, long id) => throw new NotSupportedException();
        public Task CapNhatAsync(long id, UpdateBodyRequest r, long user) => throw new NotSupportedException();
    }

    [Theory]
    [InlineData("NHA_XAC", "ALL", true)]
    [InlineData("CONG_AN", "PARTIAL", true)]
    [InlineData("CONG_AN", "ALL", false)]
    public async Task ServiceUsesTrustedScope(string role, string scope, bool restricted)
    {
        var previous = AccessControl.PrincipalProvider;
        AccessControl.PrincipalProvider = () => new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim("sub", "1"), new Claim("role", role), new Claim("perm", "BODY:VIEW:" + scope) }, "test"));
        try
        {
            var repo = new Repository();
            var service = new BodyService(repo);
            await service.LayDanhSachAsync(new BodyFilterRequest());
            Assert.Equal(restricted, repo.Restricted);
            using var form = new DeathManagement.API.Forms.FrmQuanLyThiThe();
            var grid = Assert.Single(form.Controls.OfType<System.Windows.Forms.DataGridView>());
            Assert.Equal(!restricted, grid.Columns.Contains(nameof(BodyDto.DiaDiem)));
            Assert.Equal(!restricted, grid.Columns.Contains(nameof(BodyDto.DacDiemCoThe)));
            Assert.Equal(restricted, grid.Columns.Contains(nameof(BodyDto.Khu)));
            await Assert.ThrowsAsync<ArgumentException>(() => service.LayDanhSachAsync(new BodyFilterRequest { Trang = 0 }));
            await Assert.ThrowsAsync<ArgumentException>(() => service.LayDanhSachAsync(new BodyFilterRequest { TuNgay = DateTime.Today, DenNgay = DateTime.Today.AddDays(-1) }));
            if (restricted)
            {
                var detail = await service.LayChiTietAsync(1);
                Assert.Null(detail!.DiaDiem);
                Assert.Null(detail.DacDiemCoThe);
                Assert.Null(detail.ChieuCao);
            }
        }
        finally { AccessControl.PrincipalProvider = previous; }
    }

    [Fact]
    public async Task MissingPermissionIsRejected()
    {
        var previous = AccessControl.PrincipalProvider;
        AccessControl.PrincipalProvider = () => new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("sub", "1") }, "test"));
        try { await Assert.ThrowsAsync<ForbiddenException>(() => new BodyService(new Repository()).LayDanhSachAsync(new BodyFilterRequest())); }
        finally { AccessControl.PrincipalProvider = previous; }
    }

    [Fact]
    [Trait("Category", "LocalDatabase")]
    public async Task SqlServerListFiltersAndRestrictedView()
    {
        var connection = Environment.GetEnvironmentVariable("QLTT_TEST_DB");
        if (connection == null) return; // shortcut: opt-in read-only LocalDB check, set QLTT_TEST_DB in SQL integration runs.
        using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options);
        var repo = new BodyRepository(db);
        var all = await repo.LayDanhSachAsync(new BodyFilterRequest(), false);
        var restricted = await repo.LayDanhSachAsync(new BodyFilterRequest(), true);
        Assert.Equal(all.Total, restricted.Total);
        Assert.All(restricted.Items, x => { Assert.Null(x.DiaDiem); Assert.Null(x.DacDiemCoThe); Assert.Null(x.ChieuCao); Assert.Null(x.MaTinBao); });
        if (all.Items.FirstOrDefault() is { } first)
        {
            var filtered = await repo.LayDanhSachAsync(new BodyFilterRequest { TuKhoa = first.MaHoSoThiThe.ToString(), TrangThai = first.TrangThai,
                GioiTinh = first.GioiTinh, TuNgay = first.ThoiGianPhatHien, DenNgay = first.ThoiGianPhatHien }, false);
            Assert.Contains(filtered.Items, x => x.MaHoSoThiThe == first.MaHoSoThiThe);
            var page = await repo.LayDanhSachAsync(new BodyFilterRequest { SoLuong = 1 }, false);
            Assert.Single(page.Items);
            Assert.Equal(all.Total, page.Total);
            var secondPage = await repo.LayDanhSachAsync(new BodyFilterRequest { Trang = 2, SoLuong = 1 }, false);
            Assert.DoesNotContain(secondPage.Items, x => x.MaHoSoThiThe == page.Items[0].MaHoSoThiThe);
            if (!string.IsNullOrWhiteSpace(first.DacDiemCoThe) && !long.TryParse(first.DacDiemCoThe, out _))
            {
                var hiddenSearch = await repo.LayDanhSachAsync(new BodyFilterRequest { TuKhoa = first.DacDiemCoThe }, true);
                Assert.Empty(hiddenSearch.Items);
                Assert.Equal(0, hiddenSearch.Total);
            }
        }
    }
}
