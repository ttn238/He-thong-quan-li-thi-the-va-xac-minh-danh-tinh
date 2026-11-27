using System.Security.Claims;
using DeathManagement.API.Controllers;
using DeathManagement.API.Data;
using DeathManagement.API.DTOs.Body;
using DeathManagement.API.Entities;
using DeathManagement.API.Forms;
using DeathManagement.API.Repositories;
using DeathManagement.API.Repositories.Interfaces;
using DeathManagement.API.Security;
using DeathManagement.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeathManagement.Tests;

[Collection("Body access")]
public class BodyDetailTests
{
    private sealed class FakeBodyRepository : IBodyRepository
    {
        public bool ReturnNull { get; set; }
        public long? CapturedAccountId { get; private set; }
        public bool CapturedNhaXac { get; private set; }
        public bool CapturedDoVat { get; private set; }
        public bool CapturedHinhAnh { get; private set; }
        public bool CapturedDoiSanh { get; private set; }

        public Task<BodyDetailDto?> LayChiTietDayDuAsync(
            long maHoSoThiThe,
            long maTaiKhoan,
            bool nhaXac,
            bool coQuyenXemDoVat,
            bool coQuyenXemHinhAnh,
            bool coQuyenXemDoiSanh)
        {
            CapturedAccountId = maTaiKhoan;
            CapturedNhaXac    = nhaXac;
            CapturedDoVat     = coQuyenXemDoVat;
            CapturedHinhAnh   = coQuyenXemHinhAnh;
            CapturedDoiSanh   = coQuyenXemDoiSanh;

            if (ReturnNull)
                return Task.FromResult<BodyDetailDto?>(null);

            // Cố tình trả về đầy đủ dữ liệu nhạy cảm để kiểm chứng tầng Service luôn lọc sạch khi restricted
            return Task.FromResult<BodyDetailDto?>(new BodyDetailDto
            {
                MaHoSoThiThe     = maHoSoThiThe,
                ThoiGianPhatHien = new DateTime(2026, 9, 20, 22, 30, 0),
                DiaDiem          = "Chân cầu Nhật Tân, Hà Nội",
                TrangThai        = Body.StatusDangXacMinh,
                GioiTinh         = "Nam",
                TuoiUocLuong     = 35,
                ChieuCao         = 170m,
                DacDiemCoThe     = "Vết sẹo dài 5cm ở cẳng tay trái",
                MaTinBao         = 10,
                MaViTri          = 1,
                Khu              = "Khu A",
                Tu               = "Tủ 01",
                Ngan             = "Ngăn 01",
                TinBao           = new BodyIncidentSummaryDto
                {
                    MaTinBao = 10,
                    MaTin = "TB000010",
                    TenLoaiTinBao = "Phát hiện thi thể",
                    TieuDe = "Phát hiện thi thể gần cầu",
                    NoiDung = "Người dân phát hiện lúc 22h",
                    ThoiGianXayRa = new DateTime(2026, 9, 20, 22, 0, 0),
                    DiaDiem = "Chân cầu Nhật Tân",
                    NguonTin = "NguoiDan",
                    MucDoUuTien = "KhanCap",
                    TrangThai = "DaTiepNhan",
                    NgayTiepNhan = new DateTime(2026, 9, 20, 22, 15, 0)
                },
                ViTriHienTai = new BodyStorageCurrentDto
                {
                    MaViTri = 1, Khu = "Khu A", Tu = "Tủ 01", Ngan = "Ngăn 01",
                    SucChua = 1, ThoiGianXep = new DateTime(2026, 9, 21, 1, 0, 0)
                },
                LichSuLuuTru =
                [
                    new BodyStorageHistoryDto { MaLichSu = 1, MaViTri = 1, Khu = "Khu A", Tu = "Tủ 01", Ngan = "Ngăn 01", ThoiGian = new DateTime(2026, 9, 21, 1, 0, 0) }
                ],
                DoVatCaNhan =
                [
                    new BodyPersonalItemDto { MaDoVat = 1, Ten = "Đồng hồ đeo tay", MaLoaiDoVat = 2, TenLoaiDoVat = "Trang sức", TinhTrang = "Còn hoạt động" }
                ],
                HinhAnh =
                [
                    new BodyImageItemDto { MaHinhAnh = 1, TenGoc = "anh_vet_seo.jpg", DuongDan = "/secure/test.jpg", LoaiHinhAnh = "ThiThe_KhamNghiem", DinhDang = "image/jpeg", KichThuoc = 102400, LaNhayCam = true, NguoiTaiLen = "Nguyễn Văn An", ThoiGianTaiLen = new DateTime(2026, 9, 21, 2, 0, 0) }
                ],
                KetQuaDoiSanh =
                [
                    new BodyMatchResultDto { MaDoiSanh = 5, MaHoSoThiThe = maHoSoThiThe, MaHoSoMatTich = 1, HoTenNguoiMatTich = "Nguyễn Văn Bảo", DiemDoiSanh = 82.5m, MucDoPhuHop = "CanXacMinh", TrangThai = "ChoXuLy", NguoiYeuCau = "Nguyễn Văn An", ThoiGianYeuCau = new DateTime(2026, 9, 21, 3, 0, 0) }
                ],
                LichSuTrangThai =
                [
                    new BodyStatusHistoryDto { MaNhatKy = 1, MaHoSoThiThe = maHoSoThiThe, HanhDong = "INSERT", TrangThaiMoi = Body.StatusMoiTiepNhan, ThoiGian = new DateTime(2026, 9, 20, 23, 0, 0) }
                ]
            });
        }

        public Task<long> TaoHoSoAsync(CreateBodyRequest request, long maTaiKhoan) => throw new NotSupportedException();
        public Task<Body?> LayTheoMaAsync(long maHoSoThiThe) => throw new NotSupportedException();
        public Task<BodyDto?> LayChiTietDtoAsync(long maHoSoThiThe, bool nhaXac) => throw new NotSupportedException();
        public Task<(List<BodyDto> Items, int Total)> LayDanhSachAsync(string? tuKhoa, string? trangThai, int trang, int soLuong) => throw new NotSupportedException();
        public Task<(List<BodyDto> Items, int Total)> LayDanhSachAsync(BodyFilterRequest filter, bool nhaXac) => throw new NotSupportedException();
        public Task CapNhatAsync(long maHoSoThiThe, UpdateBodyRequest request, long maTaiKhoan) => throw new NotSupportedException();
        public Task ChuyenTrangThaiAsync(long maHoSoThiThe, string trangThaiMoi, string? lyDo, long maTaiKhoan) => throw new NotSupportedException();
        public Task<List<BodyStatusHistoryDto>> LayLichSuTrangThaiAsync(long maHoSoThiThe) => throw new NotSupportedException();
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

    private static IEnumerable<T> FindControls<T>(System.Windows.Forms.Control root) where T : System.Windows.Forms.Control
    {
        foreach (System.Windows.Forms.Control c in root.Controls)
        {
            if (c is T match) yield return match;
            foreach (var nested in FindControls<T>(c)) yield return nested;
        }
    }

    [Fact]
    public async Task Officer_GetsFullAggregatedDetail_And5TabsVisible()
    {
        var previous = AccessControl.PrincipalProvider;
        AccessControl.PrincipalProvider = () => MakePrincipal(2, AuthorizationPolicies.RoleOfficer);
        try
        {
            var repo = new FakeBodyRepository();
            var service = new BodyService(repo);

            var detail = await service.GetByIdAsync(1);

            Assert.NotNull(detail);
            Assert.Equal(2L, repo.CapturedAccountId);
            Assert.False(repo.CapturedNhaXac);
            Assert.True(repo.CapturedDoVat);
            Assert.True(repo.CapturedHinhAnh);
            Assert.True(repo.CapturedDoiSanh);

            Assert.False(detail!.CheDoHanCheNhaXac);
            Assert.NotNull(detail.TinBao);
            Assert.Equal("TB000010", detail.TinBao!.MaTin);
            Assert.NotNull(detail.ViTriHienTai);
            Assert.Equal("Khu A - Tủ 01 - Ngăn 01", detail.ViTriHienTai!.ViTriHienThi);
            Assert.Single(detail.LichSuLuuTru);
            Assert.Single(detail.DoVatCaNhan);
            Assert.Single(detail.HinhAnh);
            Assert.Single(detail.KetQuaDoiSanh);
            Assert.Single(detail.LichSuTrangThai);

            // Kiểm tra giao diện 5 Tab đầy đủ cho Cán bộ Công an
            using var form = new FrmChiTietThiThe(1);
            form.HienThiDuLieu(detail);
            var tabControl = Assert.Single(FindControls<System.Windows.Forms.TabControl>(form));
            Assert.Equal(5, tabControl.TabPages.Count);
        }
        finally { AccessControl.PrincipalProvider = previous; }
    }

    [Fact]
    public async Task MorgueRole_HidesSensitiveInvestigationFields_AndHidesMatchingTab()
    {
        var previous = AccessControl.PrincipalProvider;
        AccessControl.PrincipalProvider = () => MakePrincipal(4, AuthorizationPolicies.RoleMorgue);
        try
        {
            var repo = new FakeBodyRepository();
            var service = new BodyService(repo);

            var detail = await service.GetByIdAsync(1);

            Assert.NotNull(detail);
            Assert.True(repo.CapturedNhaXac);
            Assert.True(repo.CapturedDoVat);
            Assert.True(repo.CapturedHinhAnh);
            Assert.False(repo.CapturedDoiSanh);

            // Dữ liệu nhạy cảm & tin báo & đối sánh phải bị ẩn hoàn toàn
            Assert.True(detail!.CheDoHanCheNhaXac);
            Assert.Null(detail.DiaDiem);
            Assert.Null(detail.ChieuCao);
            Assert.Null(detail.DacDiemCoThe);
            Assert.Null(detail.MaTinBao);
            Assert.Null(detail.TinBao);
            Assert.Empty(detail.KetQuaDoiSanh);

            // Nhà xác vẫn xem được vị trí lưu trữ & đồ vật cá nhân
            Assert.NotNull(detail.ViTriHienTai);
            Assert.Single(detail.DoVatCaNhan);

            // Tab Đối sánh bị ẩn trên WinForms đối với Nhà xác (còn 4 tab)
            using var form = new FrmChiTietThiThe(1);
            form.HienThiDuLieu(detail);
            var tabControl = Assert.Single(FindControls<System.Windows.Forms.TabControl>(form));
            Assert.Equal(4, tabControl.TabPages.Count);
        }
        finally { AccessControl.PrincipalProvider = previous; }
    }

    [Fact]
    public async Task CitizenRole_IsRejectedWithForbidden()
    {
        var previous = AccessControl.PrincipalProvider;
        AccessControl.PrincipalProvider = () => MakePrincipal(5, AuthorizationPolicies.RoleCitizen);
        try
        {
            var service = new BodyService(new FakeBodyRepository());
            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetByIdAsync(1));
        }
        finally { AccessControl.PrincipalProvider = previous; }
    }

    [Fact]
    public async Task UnauthenticatedUser_IsRejected()
    {
        var previous = AccessControl.PrincipalProvider;
        AccessControl.PrincipalProvider = () => null;
        try
        {
            var service = new BodyService(new FakeBodyRepository());
            await Assert.ThrowsAsync<UnauthenticatedException>(() => service.GetByIdAsync(1));
        }
        finally { AccessControl.PrincipalProvider = previous; }
    }

    [Fact]
    public async Task Controller_GetById_ReturnsNotFoundWhenMissing_AndOkWhenFound()
    {
        var previous = AccessControl.PrincipalProvider;
        AccessControl.PrincipalProvider = () => MakePrincipal(1, AuthorizationPolicies.RoleAdmin);
        try
        {
            var repo = new FakeBodyRepository { ReturnNull = true };
            var controller = new BodyController(new BodyService(repo));

            var notFound = await controller.GetById(9999);
            Assert.IsType<NotFoundObjectResult>(notFound);

            repo.ReturnNull = false;
            var ok = await controller.GetById(1);
            var okResult = Assert.IsType<OkObjectResult>(ok);
            var payload = Assert.IsType<BodyDetailDto>(okResult.Value);
            Assert.Equal(1L, payload.MaHoSoThiThe);
            Assert.Single(payload.DoVatCaNhan);
            Assert.Single(payload.KetQuaDoiSanh);
        }
        finally { AccessControl.PrincipalProvider = previous; }
    }

    [Fact]
    [Trait("Category", "LocalDatabase")]
    public async Task SqlServer_GetByIdAsync_AggregatesAllRelatedData()
    {
        var connection = Environment.GetEnvironmentVariable("QLTT_TEST_DB");
        if (connection == null) return;

        using var db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options);

        var repo = new BodyRepository(db);
        var list = await repo.LayDanhSachAsync(new BodyFilterRequest { SoLuong = 1 }, false);
        var first = list.Items.FirstOrDefault();
        if (first == null) return;

        var full = await repo.LayChiTietDayDuAsync(first.MaHoSoThiThe, maTaiKhoan: 2, nhaXac: false, coQuyenXemDoVat: true, coQuyenXemHinhAnh: true, coQuyenXemDoiSanh: true);
        Assert.NotNull(full);
        Assert.Equal(first.MaHoSoThiThe, full!.MaHoSoThiThe);

        var restricted = await repo.LayChiTietDayDuAsync(first.MaHoSoThiThe, maTaiKhoan: 4, nhaXac: true, coQuyenXemDoVat: true, coQuyenXemHinhAnh: true, coQuyenXemDoiSanh: false);
        Assert.NotNull(restricted);
        Assert.Null(restricted!.DiaDiem);
        Assert.Null(restricted.DacDiemCoThe);
        Assert.Null(restricted.ChieuCao);
        Assert.Null(restricted.TinBao);
        Assert.Empty(restricted.KetQuaDoiSanh);
    }
}
