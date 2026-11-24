using DeathManagement.API.DTOs.Body;
using DeathManagement.API.Entities;
using DeathManagement.API.Repositories.Interfaces;
using DeathManagement.API.Security;
using DeathManagement.API.Services.Interfaces;

namespace DeathManagement.API.Services;

/// <summary>
/// Service nghiệp vụ quản lý hồ sơ thi thể (Task 6.1 — EPIC 6).
/// Mọi phương thức đều kiểm tra quyền qua AccessControl trước khi thao tác dữ liệu.
/// </summary>
public class BodyService(IBodyRepository repo) : IBodyService
{
    // ─────────────────────────── Hằng quyền ───────────────────────────
    private const string ResBody   = "BODY";
    private const string ActCreate = "CREATE";
    private const string ActView   = "VIEW";
    private const string ActUpdate = "UPDATE";

    // ───────────────────────── Tạo hồ sơ mới ─────────────────────────

    /// <inheritdoc />
    public async Task<long> TaoHoSoAsync(CreateBodyRequest request)
    {
        // 1. Kiểm tra quyền
        AccessControl.Require(ResBody, ActCreate);

        // 2. Validate nghiệp vụ
        if (request.ThoiGianPhatHien > DateTime.Now)
            throw new ArgumentException("Thời gian phát hiện không được ở tương lai.");

        if (string.IsNullOrWhiteSpace(request.DiaDiem))
            throw new ArgumentException("Địa điểm phát hiện không được để trống.");

        if (request.DiaDiem.Trim().Length > 255)
            throw new ArgumentException("Địa điểm phát hiện không được vượt quá 255 ký tự.");

        if (request.TuoiUocLuong is < 0 or > 130)
            throw new ArgumentException("Tuổi ước lượng phải từ 0 đến 130.");

        if (request.ChieuCao is <= 0 or > 300)
            throw new ArgumentException("Chiều cao phải lớn hơn 0 và không vượt quá 300 cm.");

        ValidateGioiTinh(request.GioiTinh);

        // 3. Chuẩn hóa
        request.GioiTinh = string.IsNullOrWhiteSpace(request.GioiTinh) ? "KhongXacDinh" : request.GioiTinh.Trim();
        request.DiaDiem  = request.DiaDiem.Trim();

        // 4. Gọi repository → sp_LapHoSoThiThe
        var maTaiKhoan = AccessControl.UserId
            ?? throw AccessControl.NotLoggedIn(ResBody, ActCreate);

        return await repo.TaoHoSoAsync(request, maTaiKhoan);
    }

    // ──────────────────────── Lấy chi tiết ───────────────────────────

    /// <inheritdoc />
    public async Task<BodyDto?> LayChiTietAsync(long maHoSoThiThe)
    {
        AccessControl.Require(ResBody, ActView);

        var body = await repo.LayTheoMaAsync(maHoSoThiThe);
        if (body != null && (AccessControl.RoleCode == "NHA_XAC" || AccessControl.ScopeOf(ResBody, ActView) == "PARTIAL"))
            return new BodyDto { MaHoSoThiThe = body.MaHoSoThiThe, TrangThai = body.TrangThai,
                ThoiGianPhatHien = body.ThoiGianPhatHien, GioiTinh = body.GioiTinh, TuoiUocLuong = body.TuoiUocLuong };
        return body == null ? null : MapToDto(body);
    }

    // ──────────────────────── Lấy danh sách ──────────────────────────

    /// <inheritdoc />
    public async Task<(List<BodyDto> Items, int Total)> LayDanhSachAsync(
        string? tuKhoa, string? trangThai, int trang = 1, int soLuong = 20)
        => await LayDanhSachAsync(new BodyFilterRequest
        { TuKhoa = tuKhoa, TrangThai = trangThai, Trang = trang, SoLuong = soLuong });

    public async Task<(List<BodyDto> Items, int Total)> LayDanhSachAsync(BodyFilterRequest filter)
    {
        AccessControl.Require(ResBody, ActView);
        if (AccessControl.ScopeOf(ResBody, ActView) is not ("ALL" or "PARTIAL"))
            throw new ForbiddenException("Phạm vi quyền chưa hỗ trợ xem danh sách hồ sơ.");
        if (filter.Trang < 1 || filter.SoLuong is < 1 or > 100 ||
            (long)(filter.Trang - 1) * filter.SoLuong > int.MaxValue)
            throw new ArgumentException("Trang phải từ 1 và số lượng từ 1 đến 100.");
        ValidateGioiTinh(filter.GioiTinh);
        if (filter.TrangThai != null && !new[] { "MoiTiepNhan", "DangLuuGiu", "DangXacMinh", "DaXacDinh", "DaBanGiao", "DongHoSo", "TiepTucLuuGiu" }.Contains(filter.TrangThai))
            throw new ArgumentException("Trạng thái hồ sơ không hợp lệ.");
        if (filter.TuNgay > filter.DenNgay)
            throw new ArgumentException("Thời gian bắt đầu phải trước thời gian kết thúc.");
        if (filter.TuKhoa?.Length > 255)
            throw new ArgumentException("Từ khóa tối đa 255 ký tự.");
        return await repo.LayDanhSachAsync(filter,
            AccessControl.RoleCode == "NHA_XAC" || AccessControl.ScopeOf(ResBody, ActView) == "PARTIAL");
    }

    // ────────────────────────── Cập nhật ─────────────────────────────

    /// <inheritdoc />
    public async Task CapNhatAsync(long maHoSoThiThe, UpdateBodyRequest request)
    {
        AccessControl.Require(ResBody, ActUpdate);

        if (request.ThoiGianPhatHien.HasValue && request.ThoiGianPhatHien.Value > DateTime.Now)
            throw new ArgumentException("Thời gian phát hiện không được ở tương lai.");

        if (request.GioiTinh != null)
            ValidateGioiTinh(request.GioiTinh);

        if (request.ChieuCao.HasValue && request.ChieuCao.Value <= 0)
            throw new ArgumentException("Chiều cao phải lớn hơn 0.");

        if (request.TuoiUocLuong.HasValue && (request.TuoiUocLuong < 0 || request.TuoiUocLuong > 130))
            throw new ArgumentException("Tuổi ước lượng phải từ 0 đến 130.");

        var maTaiKhoan = AccessControl.UserId
            ?? throw AccessControl.NotLoggedIn(ResBody, ActUpdate);

        await repo.CapNhatAsync(maHoSoThiThe, request, maTaiKhoan);
    }

    // ─────────────────────── Phương thức tiện ích ────────────────────

    private static void ValidateGioiTinh(string? gioiTinh)
    {
        if (gioiTinh == null) return;
        var valid = new[] { "Nam", "Nu", "Khac", "KhongXacDinh" };
        if (!valid.Contains(gioiTinh))
            throw new ArgumentException($"Giới tính không hợp lệ. Các giá trị được chấp nhận: {string.Join(", ", valid)}.");
    }

    private static BodyDto MapToDto(Body b) => new()
    {
        MaHoSoThiThe    = b.MaHoSoThiThe,
        ThoiGianPhatHien = b.ThoiGianPhatHien,
        DiaDiem         = b.DiaDiem,
        TrangThai       = b.TrangThai,
        GioiTinh        = b.GioiTinh,
        TuoiUocLuong    = b.TuoiUocLuong,
        ChieuCao        = b.ChieuCao,
        DacDiemCoThe    = b.DacDiemCoThe,
        MaTinBao        = b.MaTinBao
    };
}
