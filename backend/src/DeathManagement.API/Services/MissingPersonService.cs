using DeathManagement.API.DTOs.MissingPerson;
using DeathManagement.API.Repositories.Interfaces;
using DeathManagement.API.Security;
using DeathManagement.API.Services.Interfaces;
using static DeathManagement.API.Security.AuthorizationPolicies;

namespace DeathManagement.API.Services;

/// <summary>
/// Service nghiệp vụ hồ sơ người mất tích (Task 8.4 — Xem chi tiết).
/// Kiểm tra quyền qua AccessControl trước khi truy vấn dữ liệu.
/// </summary>
public class MissingPersonService(IMissingPersonRepository repo) : IMissingPersonService
{
    public async Task<MissingPersonDetailDto?> GetByIdAsync(long maHoSoMatTich)
    {
        AccessControl.Require(ResourceMissingPerson, ActionView);

        if (maHoSoMatTich <= 0)
            throw new ArgumentException("Mã hồ sơ người mất tích không hợp lệ.", nameof(maHoSoMatTich));

        var maTaiKhoan = AccessControl.UserId
            ?? throw AccessControl.NotLoggedIn(ResourceMissingPerson, ActionView);

        var coQuyenXemHinhAnh = AccessControl.Can(ResourceFile, ActionView);
        var coQuyenXemDoiSanh = AccessControl.Can(ResourceMatching, ActionView);

        var detail = await repo.GetDetailByIdAsync(
            maHoSoMatTich,
            maTaiKhoan,
            coQuyenXemHinhAnh,
            coQuyenXemDoiSanh);

        if (detail == null)
            return null;

        detail.CoQuyenXemHinhAnh = coQuyenXemHinhAnh;
        detail.CoQuyenXemDoiSanh = coQuyenXemDoiSanh;

        if (!coQuyenXemHinhAnh)
            detail.HinhAnh.Clear();

        if (!coQuyenXemDoiSanh)
            detail.KetQuaDoiSanh.Clear();

        return detail;
    }
}
