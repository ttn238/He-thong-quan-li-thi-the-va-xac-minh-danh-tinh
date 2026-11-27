using System.Security.Claims;
using DeathManagement.API.Controllers;
using DeathManagement.API.Data;
using DeathManagement.API.DTOs.Body;
using DeathManagement.API.DTOs.Image;
using DeathManagement.API.Helpers;
using DeathManagement.API.Security;
using DeathManagement.API.Services;
using DeathManagement.API.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeathManagement.Tests;

/// <summary>
/// Kiểm soát truy cập cho luồng tải/đọc ảnh hồ sơ thi thể (Task 6.4).
/// </summary>
[Collection("Body access")]
public class BodyImageUploadTests
{
    /// <summary>PNG 1x1 hợp lệ để đi qua bước kiểm tra nội dung tệp mà không cần đính kèm tệp.</summary>
    private const string AnhHetSiBase64 =
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==";

    private static byte[] TaoAnh() => Convert.FromBase64String(AnhHetSiBase64);

    /// <summary>
    /// DbContext không nối thật. Mọi ca kiểm thử dùng nó chặn ở bước phân quyền/định dạng,
    /// tức là TRƯỚC câu lệnh SQL đầu tiên; timeout ngắn để không treo nếu giả định này sai.
    /// </summary>
    private static ApplicationDbContext TaoDbKhongNoi() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=127.0.0.1;Database=khong_co_that;User Id=u;Password=p;Connect Timeout=1;TrustServerCertificate=True")
            .Options);

    private sealed class FakeImageService : IImageService
    {
        public UploadImageRequest? DaNhanYeuCau { get; private set; }
        public string? DaNhanTenFile { get; private set; }
        public long DaNhanSoByte { get; private set; }
        public (long MaHoSo, long MaHinh)? DaYeuCauDoc { get; private set; }
        public (byte[] DuLieu, string DinhDang)? KetQuaDoc { get; set; }

        public Task<ImageDto> TaiLenHinhAnhAsync(UploadImageRequest req, IProgress<int>? progress = null)
            => throw new NotSupportedException();

        public async Task<ImageDto> TaiLenHinhAnhTuStreamAsync(
            Stream nguon, string tenFileTaiLen, UploadImageRequest req, IProgress<int>? progress = null)
        {
            DaNhanYeuCau = req;
            DaNhanTenFile = tenFileTaiLen;

            long soByte = 0;
            var dem = new byte[8192];
            while (true)
            {
                int read = await nguon.ReadAsync(dem);
                if (read <= 0) break;
                soByte += read;
            }
            DaNhanSoByte = soByte;

            return new ImageDto(7, "tenfile.png", tenFileTaiLen, "/secure/tenfile.png", req.LoaiHinhAnh,
                "image/png", soByte, 1, 1, "hash", true, req.MoTa, req.MaHoSoThiThe, null, null, null,
                req.MaTaiKhoanTaiLen, "Cán bộ", new DateTime(2026, 10, 5, 8, 0, 0), "HoatDong");
        }

        public Task<List<ImageDto>> LayDanhSachHinhAnhAsync(ImageFilterRequest filter, bool coQuyenXemNhayCam)
            => Task.FromResult(new List<ImageDto>());

        public Task<bool> XoaHinhAnhAsync(long maHinhAnh, long maTaiKhoan) => Task.FromResult(true);

        public string LayDuongDanVatLy(string duongDanLuuTru) => duongDanLuuTru;

        public Task<byte[]?> DocDuLieuHinhAnhAsync(long maHinhAnh, bool coQuyenXemNhayCam)
            => Task.FromResult<byte[]?>(TaoAnh());

        public Task<(byte[] DuLieu, string DinhDang)?> DocAnhCuaHoSoThiTheAsync(long maHoSoThiThe, long maHinhAnh)
        {
            DaYeuCauDoc = (maHoSoThiThe, maHinhAnh);
            return Task.FromResult(KetQuaDoc);
        }
    }

    private sealed class FakeBodyService : IBodyService
    {
        public BodyDto? ChiTiet { get; init; }
        public long? MaDaYeuCau { get; private set; }

        public Task<BodyDto?> LayChiTietAsync(long maHoSoThiThe)
        {
            MaDaYeuCau = maHoSoThiThe;
            return Task.FromResult(ChiTiet);
        }

        public Task<long> TaoHoSoAsync(CreateBodyRequest request) => throw new NotSupportedException();
        public Task<BodyDetailDto?> GetByIdAsync(long maHoSoThiThe) => throw new NotSupportedException();
        public Task<(List<BodyDto> Items, int Total)> LayDanhSachAsync(string? tuKhoa, string? trangThai, int trang = 1, int soLuong = 20) => throw new NotSupportedException();
        public Task<(List<BodyDto> Items, int Total)> LayDanhSachAsync(BodyFilterRequest filter) => throw new NotSupportedException();
        public Task CapNhatAsync(long maHoSoThiThe, UpdateBodyRequest request) => throw new NotSupportedException();
        public Task<BodyDto> ChuyenTrangThaiAsync(long maHoSoThiThe, string trangThaiMoi, string? lyDo = null) => throw new NotSupportedException();
        public Task<List<BodyStatusHistoryDto>> LayLichSuTrangThaiAsync(long maHoSoThiThe) => throw new NotSupportedException();
    }

    private static IFormFile AnhForm(string tenFile, long? daiGia = null)
    {
        var noiDung = TaoAnh();
        return new FormFile(new MemoryStream(noiDung), 0, daiGia ?? noiDung.LongLength, "file", tenFile)
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/jpeg"
        };
    }

    private static ClaimsPrincipal MakePrincipal(long userId, string roleCode)
    {
        var claims = new List<Claim>
        {
            new("sub", userId.ToString()),
            new("role", roleCode)
        };
        foreach (var perm in AuthorizationPolicies.BuildPermissionClaims(roleCode))
            claims.Add(new Claim("perm", perm));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    private static IDisposable DangNhap(long userId, string roleCode)
    {
        var truocDo = AccessControl.PrincipalProvider;
        AccessControl.PrincipalProvider = () => MakePrincipal(userId, roleCode);
        return new KetThucPhien(() => AccessControl.PrincipalProvider = truocDo);
    }

    private sealed class KetThucPhien(Action action) : IDisposable
    {
        public void Dispose() => action();
    }

    // ─────────────────── Controller: kiểm tra đầu vào trước khi ghi tệp ───────────────────

    [Fact]
    public async Task Upload_NoFile_ReturnsBadRequest_AndNeverWrites()
    {
        using var _ = DangNhap(2, AuthorizationPolicies.RoleOfficer);
        var images = new FakeImageService();
        var controller = new BodyController(new FakeBodyService { ChiTiet = new BodyDto { MaHoSoThiThe = 5 } }, images);

        var result = await controller.UploadImage(5, file: null, loaiHinhAnh: null, moTa: null);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Null(images.DaNhanYeuCau);
    }

    [Fact]
    public async Task Upload_OversizedFile_Returns413_AndNeverWrites()
    {
        using var _ = DangNhap(2, AuthorizationPolicies.RoleOfficer);
        var images = new FakeImageService();
        var controller = new BodyController(new FakeBodyService { ChiTiet = new BodyDto { MaHoSoThiThe = 5 } }, images);
        var file = AnhForm("qua_nang.jpg", FileHelper.MaxFileSizeInBytes + 1);

        var result = await controller.UploadImage(5, file, null, null);

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status413PayloadTooLarge, status.StatusCode);
        Assert.Null(images.DaNhanYeuCau);
    }

    [Fact]
    public async Task Upload_MissingBody_Returns404_AndNeverWrites()
    {
        using var _ = DangNhap(2, AuthorizationPolicies.RoleOfficer);
        var bodies = new FakeBodyService { ChiTiet = null };
        var images = new FakeImageService();
        var controller = new BodyController(bodies, images);

        var result = await controller.UploadImage(9999, AnhForm("vet.jpg"), null, null);

        Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(9999L, bodies.MaDaYeuCau);
        Assert.Null(images.DaNhanYeuCau);
    }

    [Fact]
    public async Task Upload_ForcesBodyIdFromRoute_IgnoresPayloadLinks()
    {
        using var _ = DangNhap(2, AuthorizationPolicies.RoleOfficer);
        var images = new FakeImageService();
        var controller = new BodyController(new FakeBodyService { ChiTiet = new BodyDto { MaHoSoThiThe = 42 } }, images);

        var result = await controller.UploadImage(42, AnhForm("vet.jpg"), null, "  Ảnh khám nghiệm  ");

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.NotNull(images.DaNhanYeuCau);
        Assert.Equal(42L, images.DaNhanYeuCau!.MaHoSoThiThe);
        Assert.Null(images.DaNhanYeuCau.MaTinBao);
        Assert.Null(images.DaNhanYeuCau.MaHoSoMatTich);
        Assert.Null(images.DaNhanYeuCau.MaXacMinh);
        Assert.Equal(ImageHelper.TypeBodyExamination, images.DaNhanYeuCau.LoaiHinhAnh);
        Assert.Equal("Ảnh khám nghiệm", images.DaNhanYeuCau.MoTa);
        Assert.Equal("vet.jpg", images.DaNhanTenFile);
        Assert.Equal(TaoAnh().LongLength, images.DaNhanSoByte);
        Assert.Equal(42L, created.RouteValues!["id"]);
        Assert.Equal(7L, created.RouteValues["imageId"]);
    }

    [Fact]
    public async Task Upload_TrimsAndAcceptsKnownImageType()
    {
        using var _ = DangNhap(2, AuthorizationPolicies.RoleOfficer);
        var images = new FakeImageService();
        var controller = new BodyController(new FakeBodyService { ChiTiet = new BodyDto { MaHoSoThiThe = 42 } }, images);

        await controller.UploadImage(42, AnhForm("toan_than.jpg"), ImageHelper.TypeBodyFull, null);

        Assert.Equal(ImageHelper.TypeBodyFull, images.DaNhanYeuCau!.LoaiHinhAnh);
    }

    [Fact]
    public async Task Upload_DescriptionTooLong_ReturnsBadRequest()
    {
        using var _ = DangNhap(2, AuthorizationPolicies.RoleOfficer);
        var images = new FakeImageService();
        var controller = new BodyController(new FakeBodyService { ChiTiet = new BodyDto { MaHoSoThiThe = 5 } }, images);

        var result = await controller.UploadImage(5, AnhForm("a.jpg"), null, new string('x', 501));

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Null(images.DaNhanYeuCau);
    }

    // ─────────────────── Controller: phục vụ tệp ảnh theo hồ sơ ───────────────────

    [Fact]
    public async Task GetImage_NotFoundOrNoRight_Returns404()
    {
        using var _ = DangNhap(4, AuthorizationPolicies.RoleMorgue);
        var images = new FakeImageService { KetQuaDoc = null };
        var controller = new BodyController(new FakeBodyService(), images);

        var result = await controller.GetImage(5, 77);

        Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal((5L, 77L), images.DaYeuCauDoc);
    }

    [Fact]
    public async Task GetImage_ReturnsBytesWithStoredMimeType()
    {
        using var _ = DangNhap(4, AuthorizationPolicies.RoleMorgue);
        var noiDung = TaoAnh();
        var images = new FakeImageService { KetQuaDoc = (noiDung, "image/png") };
        var controller = new BodyController(new FakeBodyService(), images);

        var result = await controller.GetImage(5, 77);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("image/png", file.ContentType);
        Assert.Equal(noiDung, file.FileContents);
    }

    // ─────────────────── Service: quyền thật, chặn trước mọi câu SQL ───────────────────

    [Fact]
    public async Task StreamUpload_CitizenDeniedBecauseBodyImageIsSensitive()
    {
        using var _ = DangNhap(9, AuthorizationPolicies.RoleCitizen);
        using var db = TaoDbKhongNoi();
        var service = new ImageService(db);
        var req = new UploadImageRequest { LoaiHinhAnh = ImageHelper.TypeBodyExamination, MaHoSoThiThe = 5 };

        await using var stream = new MemoryStream(TaoAnh());
        var ex = await Assert.ThrowsAsync<ForbiddenException>(
            () => service.TaiLenHinhAnhTuStreamAsync(stream, "vet.jpg", req));

        Assert.Equal(403, ex.StatusCode);
    }

    [Fact]
    public async Task StreamUpload_AdminDeniedWithoutFileCreate()
    {
        using var _ = DangNhap(1, AuthorizationPolicies.RoleAdmin);
        using var db = TaoDbKhongNoi();
        var service = new ImageService(db);
        var req = new UploadImageRequest { LoaiHinhAnh = ImageHelper.TypeBodyExamination, MaHoSoThiThe = 5 };

        await using var stream = new MemoryStream(TaoAnh());
        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.TaiLenHinhAnhTuStreamAsync(stream, "vet.jpg", req));
    }

    [Theory]
    [InlineData("thong_bao.exe")]
    [InlineData("logo.svg")]
    [InlineData("khong_co_duoi")]
    public async Task StreamUpload_RejectsExtensionOutsideWhitelist(string tenFile)
    {
        using var _ = DangNhap(2, AuthorizationPolicies.RoleOfficer);
        using var db = TaoDbKhongNoi();
        var service = new ImageService(db);
        var req = new UploadImageRequest { LoaiHinhAnh = ImageHelper.TypeBodyExamination, MaHoSoThiThe = 5 };

        await using var stream = new MemoryStream(TaoAnh());
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.TaiLenHinhAnhTuStreamAsync(stream, tenFile, req));
    }

    [Fact]
    public async Task StreamUpload_RejectsContentThatIsNotAnImage()
    {
        using var _ = DangNhap(2, AuthorizationPolicies.RoleOfficer);
        using var db = TaoDbKhongNoi();
        var service = new ImageService(db);
        var req = new UploadImageRequest { LoaiHinhAnh = ImageHelper.TypeBodyExamination, MaHoSoThiThe = 5 };

        await using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("day khong phai du lieu anh"));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.TaiLenHinhAnhTuStreamAsync(stream, "khong_phai_anh.png", req));
    }

    [Fact]
    public async Task StreamUpload_StopsReadingOnceStreamExceedsLimit()
    {
        using var _ = DangNhap(2, AuthorizationPolicies.RoleOfficer);
        using var db = TaoDbKhongNoi();
        var service = new ImageService(db);
        var req = new UploadImageRequest { LoaiHinhAnh = ImageHelper.TypeBodyExamination, MaHoSoThiThe = 5 };

        await using var stream = new MemoryStream(new byte[FileHelper.MaxFileSizeInBytes + 1]);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.TaiLenHinhAnhTuStreamAsync(stream, "qua_nang.png", req));
    }
}
