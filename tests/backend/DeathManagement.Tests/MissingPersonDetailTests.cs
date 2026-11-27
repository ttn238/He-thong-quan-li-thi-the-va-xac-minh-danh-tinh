using System.Security.Claims;
using DeathManagement.API.Controllers;
using DeathManagement.API.Data;
using DeathManagement.API.DTOs.MissingPerson;
using DeathManagement.API.Entities;
using DeathManagement.API.Repositories;
using DeathManagement.API.Repositories.Interfaces;
using DeathManagement.API.Security;
using DeathManagement.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeathManagement.Tests;

[Collection("Body access")]
public class MissingPersonDetailTests
{
    private sealed class FakeMissingPersonRepo : IMissingPersonRepository
    {
        public bool ReturnNull { get; set; }

        public Task<MissingPersonDetailDto?> GetDetailByIdAsync(
            long maHoSoMatTich,
            long maTaiKhoan,
            bool coQuyenXemHinhAnh,
            bool coQuyenXemDoiSanh)
        {
            if (ReturnNull)
                return Task.FromResult<MissingPersonDetailDto?>(null);

            return Task.FromResult<MissingPersonDetailDto?>(new MissingPersonDetailDto
            {
                MaHoSoMatTich = maHoSoMatTich,
                HoTen = "Nguyễn Văn Bảo",
                NgaySinh = new DateTime(1991, 5, 12),
                GioiTinh = "Nam",
                ThoiGianMatTich = new DateTime(2026, 9, 20, 18, 0, 0),
                NoiMatTich = "Đường Xuân La, Hà Nội",
                TrangThai = MissingPerson.StatusDangTimKiem,
                NguoiThan =
                [
                    new MissingPersonRelativeDto
                    {
                        MaNguoiThan = 1,
                        MaHoSoMatTich = maHoSoMatTich,
                        HoTen = "Nguyễn Văn Bình",
                        QuanHe = "Anh trai",
                        SoDienThoai = "0912345678",
                        DiaChi = "Số 12 ngõ 45 Xuân La, Hà Nội"
                    }
                ],
                HinhAnh =
                [
                    new MissingPersonImageDto
                    {
                        MaHinhAnh = 3,
                        TenGoc = "chan_dung_bao.jpg",
                        DuongDan = "/public/chan_dung_bao.jpg",
                        LoaiHinhAnh = "MatTich_ChanDung",
                        DinhDang = "image/jpeg",
                        KichThuoc = 512000,
                        LaNhayCam = false,
                        ThoiGianTaiLen = new DateTime(2026, 9, 21, 9, 0, 0),
                        NguoiTaiLen = "Phạm Thị Duyên"
                    }
                ],
                KetQuaDoiSanh =
                [
                    new MissingPersonMatchDto
                    {
                        MaDoiSanh = 5,
                        MaHoSoThiThe = 1,
                        MaHoSoMatTich = maHoSoMatTich,
                        HoTenNguoiMatTich = "Nguyễn Văn Bảo",
                        DiemDoiSanh = 86.5m,
                        MucDoPhuHop = "CanXacMinh",
                        TrangThai = "ChoXuLy",
                        TieuChi = "Khuôn mặt + vết sẹo cẳng tay",
                        ThoiGianYeuCau = new DateTime(2026, 9, 22, 10, 0, 0),
                        NguoiYeuCau = "Nguyễn Văn An"
                    }
                ]
            });
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
    public async Task OfficerGetsFullDetailWithRelativesImagesAndMatches()
    {
        using var _ = WithPrincipal("CONG_AN", "MISSING_PERSON:VIEW:ALL", "FILE:VIEW:ALL", "MATCHING:VIEW:ALL");
        var controller = new MissingPersonController(new MissingPersonService(new FakeMissingPersonRepo()));

        var res = await controller.GetById(1);
        var ok = Assert.IsType<OkObjectResult>(res.Result);
        var dto = Assert.IsType<MissingPersonDetailDto>(ok.Value);

        Assert.Equal("Nguyễn Văn Bảo", dto.HoTen);
        Assert.Single(dto.NguoiThan);
        Assert.Single(dto.HinhAnh);
        Assert.Single(dto.KetQuaDoiSanh);
        Assert.True(dto.CoQuyenXemHinhAnh);
        Assert.True(dto.CoQuyenXemDoiSanh);
    }

    [Fact]
    public async Task CitizenWithoutMatchingPermissionCannotSeeMatches()
    {
        using var _ = WithPrincipal("NGUOI_DAN", "MISSING_PERSON:VIEW:OWN", "FILE:VIEW:OWN");
        var service = new MissingPersonService(new FakeMissingPersonRepo());

        var dto = await service.GetByIdAsync(1);
        Assert.NotNull(dto);
        Assert.Single(dto!.NguoiThan);
        Assert.Single(dto.HinhAnh);
        Assert.Empty(dto.KetQuaDoiSanh);
        Assert.False(dto.CoQuyenXemDoiSanh);
    }

    [Fact]
    public async Task InvalidIdNotFoundAndUnauthorizedAreHandled()
    {
        using (WithPrincipal("CONG_AN", "MISSING_PERSON:VIEW:ALL"))
        {
            var controller = new MissingPersonController(new MissingPersonService(new FakeMissingPersonRepo { ReturnNull = true }));
            Assert.IsType<BadRequestObjectResult>((await controller.GetById(0)).Result);
            Assert.IsType<NotFoundObjectResult>((await controller.GetById(9999)).Result);
        }

        var service = new MissingPersonService(new FakeMissingPersonRepo());
        using (WithPrincipal(null))
        {
            await Assert.ThrowsAsync<UnauthenticatedException>(() => service.GetByIdAsync(1));
        }

        using (WithPrincipal("NHA_XAC", "STORAGE:VIEW:ALL"))
        {
            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetByIdAsync(1));
        }
    }

    [Fact]
    [Trait("Category", "LocalDatabase")]
    public async Task SqlServerGetDetailLoadsRealSeedRecordRelativesAndImages()
    {
        var connection = Environment.GetEnvironmentVariable("QLTT_TEST_DB");
        if (connection == null) return;

        using var _ = WithPrincipal("CONG_AN", "MISSING_PERSON:VIEW:ALL", "FILE:VIEW:ALL", "MATCHING:VIEW:ALL");
        await using var db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options);

        var first = await db.MissingPersons.AsNoTracking().OrderBy(x => x.MaHoSoMatTich).FirstOrDefaultAsync();
        if (first == null) return;

        var service = new MissingPersonService(new MissingPersonRepository(db));
        var detail = await service.GetByIdAsync(first.MaHoSoMatTich);

        Assert.NotNull(detail);
        Assert.Equal(first.HoTen, detail!.HoTen);
        Assert.NotNull(detail.NguoiThan);
        Assert.NotNull(detail.HinhAnh);
        Assert.NotNull(detail.KetQuaDoiSanh);
    }
}
