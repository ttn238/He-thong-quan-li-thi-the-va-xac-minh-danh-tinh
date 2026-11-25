using DeathManagement.API.DTOs.Body;
using DeathManagement.API.Entities;
using DeathManagement.API.Repositories.Interfaces;
using DeathManagement.API.Security;
using DeathManagement.API.Services.Interfaces;

namespace DeathManagement.API.Services;

/// <summary>
/// Service nghiệp vụ quản lý hồ sơ thi thể và điều phối chuyển đổi trạng thái (Task 6.1 — EPIC 6).
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

        // 3. Chuẩn hóa & kiểm tra giới tính
        request.GioiTinh = string.IsNullOrWhiteSpace(request.GioiTinh) ? "KhongXacDinh" : request.GioiTinh.Trim();
        request.DiaDiem  = request.DiaDiem.Trim();
        ValidateGioiTinh(request.GioiTinh);

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

        var restricted = IsRestrictedScope();
        var dto = await repo.LayChiTietDtoAsync(maHoSoThiThe, restricted);
        if (dto != null) return dto;

        var body = await repo.LayTheoMaAsync(maHoSoThiThe);
        if (body == null) return null;

        if (restricted)
        {
            return new BodyDto
            {
                MaHoSoThiThe     = body.MaHoSoThiThe,
                TrangThai        = body.TrangThai,
                ThoiGianPhatHien = body.ThoiGianPhatHien,
                GioiTinh         = body.GioiTinh,
                TuoiUocLuong     = body.TuoiUocLuong
            };
        }

        return MapToDto(body);
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
        if (!string.IsNullOrWhiteSpace(filter.TrangThai) && !Body.IsValidStatus(filter.TrangThai))
            throw new ArgumentException("Trạng thái hồ sơ không hợp lệ.");
        if (filter.TuNgay > filter.DenNgay)
            throw new ArgumentException("Thời gian bắt đầu phải trước thời gian kết thúc.");
        if (filter.TuKhoa?.Length > 255)
            throw new ArgumentException("Từ khóa tối đa 255 ký tự.");
        return await repo.LayDanhSachAsync(filter, IsRestrictedScope());
    }

    // ────────────────────────── Cập nhật ─────────────────────────────

    /// <inheritdoc />
    public async Task CapNhatAsync(long maHoSoThiThe, UpdateBodyRequest request)
    {
        AccessControl.Require(ResBody, ActUpdate);

        if (request.ThoiGianPhatHien.HasValue && request.ThoiGianPhatHien.Value > DateTime.Now)
            throw new ArgumentException("Thời gian phát hiện không được ở tương lai.");

        if (request.DiaDiem != null && request.DiaDiem.Trim().Length > 255)
            throw new ArgumentException("Địa điểm phát hiện không được vượt quá 255 ký tự.");

        if (request.GioiTinh != null)
        {
            request.GioiTinh = string.IsNullOrWhiteSpace(request.GioiTinh) ? "KhongXacDinh" : request.GioiTinh.Trim();
            ValidateGioiTinh(request.GioiTinh);
        }

        if (request.ChieuCao.HasValue && (request.ChieuCao.Value <= 0 || request.ChieuCao.Value > 300))
            throw new ArgumentException("Chiều cao phải lớn hơn 0 và không vượt quá 300 cm.");

        if (request.TuoiUocLuong.HasValue && (request.TuoiUocLuong < 0 || request.TuoiUocLuong > 130))
            throw new ArgumentException("Tuổi ước lượng phải từ 0 đến 130.");

        if (!string.IsNullOrWhiteSpace(request.TrangThai))
        {
            ValidateStatusAndRolePermission(request.TrangThai.Trim());
        }

        // Nhân viên nhà xác (PARTIAL) không được sửa các trường thông tin điều tra nhạy cảm
        if (IsRestrictedUpdateScope() && (request.DiaDiem != null || request.DacDiemCoThe != null || request.ThoiGianPhatHien.HasValue))
        {
            throw AccessControl.NoPermission(AccessControl.Principal, ResBody, ActUpdate,
                "Nhân viên nhà xác chỉ được phép cập nhật trạng thái lưu giữ, không được sửa địa điểm hoặc đặc điểm nhận dạng.");
        }

        var maTaiKhoan = AccessControl.UserId
            ?? throw AccessControl.NotLoggedIn(ResBody, ActUpdate);

        await repo.CapNhatAsync(maHoSoThiThe, request, maTaiKhoan);
    }

    // ───────────────────── Chuyển trạng thái hồ sơ ─────────────────────

    /// <inheritdoc />
    public async Task<BodyDto> ChuyenTrangThaiAsync(long maHoSoThiThe, string trangThaiMoi, string? lyDo = null)
    {
        AccessControl.Require(ResBody, ActUpdate);

        if (string.IsNullOrWhiteSpace(trangThaiMoi))
            throw new ArgumentException("Vui lòng chọn trạng thái mới cho hồ sơ.");

        var dich = trangThaiMoi.Trim();
        ValidateStatusAndRolePermission(dich);

        var maTaiKhoan = AccessControl.UserId
            ?? throw AccessControl.NotLoggedIn(ResBody, ActUpdate);

        var current = await repo.LayTheoMaAsync(maHoSoThiThe)
            ?? throw new KeyNotFoundException($"Không tìm thấy hồ sơ thi thể #{maHoSoThiThe}.");

        if (current.TrangThai == dich)
            throw new InvalidOperationException($"Hồ sơ #{maHoSoThiThe} đang ở trạng thái '{Body.GetStatusLabel(dich)}'.");

        if (!Body.CanTransition(current.TrangThai, dich))
        {
            var nextAllowed = Body.GetAllowedNextStatuses(current.TrangThai);
            var nextDisplay = nextAllowed.Count > 0
                ? string.Join(", ", nextAllowed.Select(s => $"{Body.GetStatusLabel(s)} ({s})"))
                : "Không có";
            throw new InvalidOperationException(
                $"Chuyển trạng thái không hợp lệ: Không thể chuyển từ '{Body.GetStatusLabel(current.TrangThai)}' ({current.TrangThai}) sang '{Body.GetStatusLabel(dich)}' ({dich}). Trạng thái hợp lệ tiếp theo: {nextDisplay}.");
        }

        if (current.TrangThai == Body.StatusDongHoSo && dich == Body.StatusTiepTucLuuGiu && string.IsNullOrWhiteSpace(lyDo))
            throw new ArgumentException("Mở lại hồ sơ đã đóng bắt buộc phải nhập lý do.");

        await repo.ChuyenTrangThaiAsync(maHoSoThiThe, dich, lyDo, maTaiKhoan);

        return await LayChiTietAsync(maHoSoThiThe)
               ?? MapToDto(new Body { MaHoSoThiThe = maHoSoThiThe, TrangThai = dich, ThoiGianPhatHien = current.ThoiGianPhatHien });
    }

    // ───────────────────── Lịch sử trạng thái hồ sơ ─────────────────────

    /// <inheritdoc />
    public async Task<List<BodyStatusHistoryDto>> LayLichSuTrangThaiAsync(long maHoSoThiThe)
    {
        AccessControl.Require(ResBody, ActView);

        _ = await repo.LayTheoMaAsync(maHoSoThiThe)
            ?? throw new KeyNotFoundException($"Không tìm thấy hồ sơ thi thể #{maHoSoThiThe}.");

        return await repo.LayLichSuTrangThaiAsync(maHoSoThiThe);
    }

    // ─────────────────────── Phương thức tiện ích ────────────────────

    private static bool IsRestrictedScope()
        => AccessControl.RoleCode == AuthorizationPolicies.RoleMorgue
           || AccessControl.ScopeOf(ResBody, ActView) == AuthorizationPolicies.ScopePartial;

    private static bool IsRestrictedUpdateScope()
        => AccessControl.RoleCode == AuthorizationPolicies.RoleMorgue
           || AccessControl.ScopeOf(ResBody, ActUpdate) == AuthorizationPolicies.ScopePartial;

    private static void ValidateStatusAndRolePermission(string status)
    {
        if (!Body.IsValidStatus(status))
            throw new ArgumentException(
                $"Trạng thái hồ sơ '{status}' không hợp lệ. Các giá trị hợp lệ: {string.Join(", ", Body.ValidStatuses)}.");

        // Nếu vai trò NHA_XAC (phạm vi PARTIAL): chỉ được chuyển sang các trạng thái liên quan đến lưu giữ hoặc bàn giao tại nhà xác
        if (IsRestrictedUpdateScope() && status is Body.StatusDaXacDinh or Body.StatusDongHoSo)
        {
            throw AccessControl.NoPermission(AccessControl.Principal, ResBody, ActUpdate,
                $"Vai trò Nhân viên nhà xác không được phép chốt trạng thái '{Body.GetStatusLabel(status)}'. Thao tác này thuộc thẩm quyền Cán bộ xác minh.");
        }
    }

    private static void ValidateGioiTinh(string? gioiTinh)
    {
        if (string.IsNullOrWhiteSpace(gioiTinh)) return;
        var valid = new[] { "Nam", "Nu", "Khac", "KhongXacDinh" };
        if (!valid.Contains(gioiTinh.Trim()))
            throw new ArgumentException($"Giới tính không hợp lệ. Các giá trị được chấp nhận: {string.Join(", ", valid)}.");
    }

    private static BodyDto MapToDto(Body b) => new()
    {
        MaHoSoThiThe     = b.MaHoSoThiThe,
        ThoiGianPhatHien = b.ThoiGianPhatHien,
        DiaDiem          = b.DiaDiem,
        TrangThai        = b.TrangThai,
        GioiTinh         = b.GioiTinh,
        TuoiUocLuong     = b.TuoiUocLuong,
        ChieuCao         = b.ChieuCao,
        DacDiemCoThe     = b.DacDiemCoThe,
        MaTinBao         = b.MaTinBao
    };
}
