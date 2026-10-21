using DeathManagement.API.DTOs.User;
using DeathManagement.API.Entities;
using DeathManagement.API.Repositories;
using DeathManagement.API.Repositories.Interfaces;
using DeathManagement.API.Security;
using DeathManagement.API.Services.Interfaces;
using static DeathManagement.API.Security.AuthorizationPolicies;

namespace DeathManagement.API.Services;

/// <summary>
/// Dịch vụ nghiệp vụ quản lý tài khoản người dùng
/// </summary>
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository? userRepository = null)
    {
        _userRepository = userRepository ?? new UserRepository();
    }

    public async Task<PagedResult<UserDto>> GetPagedListAsync(UserFilterRequest filter)
    {
        var pagedUsers = await _userRepository.GetPagedListAsync(filter);

        var dtos = pagedUsers.Items.Select(MapToDto).ToList();

        return new PagedResult<UserDto>
        {
            Items = dtos,
            TotalCount = pagedUsers.TotalCount,
            PageIndex = pagedUsers.PageIndex,
            PageSize = pagedUsers.PageSize
        };
    }

    public async Task<UserDto?> GetByIdAsync(long id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        return user == null ? null : MapToDto(user);
    }

    public async Task<UserDto?> GetByUsernameAsync(string username)
    {
        var user = await _userRepository.GetByUsernameAsync(username);
        return user == null ? null : MapToDto(user);
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request)
    {
        var username = request.TenDangNhap.Trim();

        // Kiểm tra trùng lặp
        if (await _userRepository.ExistsByUsernameAsync(username))
            throw new InvalidOperationException($"Tên đăng nhập '{username}' đã được sử dụng trong hệ thống.");

        if (!string.IsNullOrWhiteSpace(request.Email) && await _userRepository.ExistsByEmailAsync(request.Email.Trim()))
            throw new InvalidOperationException($"Email '{request.Email}' đã được liên kết với một tài khoản khác.");

        if (!string.IsNullOrWhiteSpace(request.SoDienThoai) && await _userRepository.ExistsByPhoneAsync(request.SoDienThoai.Trim()))
            throw new InvalidOperationException($"Số điện thoại '{request.SoDienThoai}' đã được liên kết với một tài khoản khác.");

        if (!string.IsNullOrWhiteSpace(request.CCCD) && await _userRepository.ExistsByCccdAsync(request.CCCD.Trim()))
            throw new InvalidOperationException($"Số CCCD/Định danh '{request.CCCD}' đã được sử dụng bởi tài khoản khác.");

        // Băm mật khẩu bằng PBKDF2 chuẩn bảo mật
        string passwordHash = PasswordHasher.HashPassword(request.MatKhau);

        var newUser = new User
        {
            TenDangNhap = username,
            MatKhauHash = passwordHash,
            HoTen = request.HoTen.Trim(),
            Email = request.Email?.Trim(),
            SoDienThoai = request.SoDienThoai?.Trim(),
            CCCD = request.CCCD?.Trim(),
            DonVi = request.DonVi?.Trim(),
            ChucVu = request.ChucVu?.Trim(),
            MaVaiTro = request.MaVaiTro,
            TrangThai = "HoatDong",
            SoLanDangNhapSai = 0,
            NgayTao = DateTime.UtcNow,
            NgayCapNhat = DateTime.UtcNow
        };

        var created = await _userRepository.CreateAsync(newUser);
        return MapToDto(created);
    }

    public async Task<UserDto?> UpdateAsync(long id, UpdateUserRequest request)
    {
        var existing = await _userRepository.GetByIdAsync(id);
        if (existing == null) return null;

        // Kiểm tra trùng lặp email, sdt, cccd ngoại trừ chính user này
        if (!string.IsNullOrWhiteSpace(request.Email) && await _userRepository.ExistsByEmailAsync(request.Email.Trim(), id))
            throw new InvalidOperationException($"Email '{request.Email}' đã được sử dụng bởi tài khoản khác.");

        if (!string.IsNullOrWhiteSpace(request.SoDienThoai) && await _userRepository.ExistsByPhoneAsync(request.SoDienThoai.Trim(), id))
            throw new InvalidOperationException($"Số điện thoại '{request.SoDienThoai}' đã được sử dụng bởi tài khoản khác.");

        if (!string.IsNullOrWhiteSpace(request.CCCD) && await _userRepository.ExistsByCccdAsync(request.CCCD.Trim(), id))
            throw new InvalidOperationException($"Số CCCD '{request.CCCD}' đã được sử dụng bởi tài khoản khác.");

        existing.HoTen = request.HoTen.Trim();
        existing.Email = request.Email?.Trim();
        existing.SoDienThoai = request.SoDienThoai?.Trim();
        existing.CCCD = request.CCCD?.Trim();
        existing.DonVi = request.DonVi?.Trim();
        existing.ChucVu = request.ChucVu?.Trim();
        existing.MaVaiTro = request.MaVaiTro;
        existing.NgayCapNhat = DateTime.UtcNow;

        var updated = await _userRepository.UpdateAsync(existing);
        return MapToDto(updated);

        if (existing.MaVaiTro != request.MaVaiTro)
        {
            AccessControl.Require(ResourcePermission, ActionUpdate);   // đổi vai trò = phân quyền
            if (AccessControl.UserId == id)
                throw new InvalidOperationException("Không được tự thay đổi vai trò của chính mình.");
        }
    }

    public async Task<UserDto?> UpdateProfileAsync(long currentUserId, UpdateProfileRequest request)
    {
        var existing = await _userRepository.GetByIdAsync(currentUserId);
        if (existing == null) return null;

        if (!string.IsNullOrWhiteSpace(request.Email) && await _userRepository.ExistsByEmailAsync(request.Email.Trim(), currentUserId))
            throw new InvalidOperationException($"Email '{request.Email}' đã được sử dụng bởi tài khoản khác.");

        if (!string.IsNullOrWhiteSpace(request.SoDienThoai) && await _userRepository.ExistsByPhoneAsync(request.SoDienThoai.Trim(), currentUserId))
            throw new InvalidOperationException($"Số điện thoại '{request.SoDienThoai}' đã được sử dụng bởi tài khoản khác.");

        existing.HoTen = request.HoTen.Trim();
        existing.Email = request.Email?.Trim();
        existing.SoDienThoai = request.SoDienThoai?.Trim();
        existing.NgayCapNhat = DateTime.UtcNow;

        var updated = await _userRepository.UpdateAsync(existing);
        return MapToDto(updated);
    }

    public async Task<bool> ChangeStatusAsync(long adminId, long targetUserId, ChangeUserStatusRequest request)
    {
        // Chống quản trị viên tự khóa tài khoản của chính mình
        if (adminId == targetUserId && (string.Equals(request.TrangThai, "Khoa", StringComparison.OrdinalIgnoreCase) ||
                                       string.Equals(request.TrangThai, "KhoaTam", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Quản trị viên không thể tự khóa tài khoản của chính mình.");
        }

        return await _userRepository.UpdateStatusAsync(targetUserId, request.TrangThai);
    }

    public async Task<bool> ResetPasswordAsync(long adminId, long targetUserId, ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.MatKhauMoi) || request.MatKhauMoi.Length < 6)
        {
            throw new InvalidOperationException("Mật khẩu mới phải có độ dài tối thiểu 6 ký tự.");
        }

        string newPasswordHash = PasswordHasher.HashPassword(request.MatKhauMoi);
        return await _userRepository.UpdatePasswordAsync(targetUserId, newPasswordHash);
    }

    public async Task<bool> DeleteAsync(long adminId, long targetUserId)
    {
        if (adminId == targetUserId)
        {
            throw new InvalidOperationException("Quản trị viên không thể tự xóa tài khoản của chính mình.");
        }

        return await _userRepository.DeleteAsync(targetUserId);
    }

    private static UserDto MapToDto(User user)
    {
        string tenVaiTro = !string.IsNullOrEmpty(user.Role?.TenVaiTro)
            ? user.Role.TenVaiTro
            : user.MaVaiTro switch
            {
                1 => "Quản trị hệ thống",
                2 => "Công an / cán bộ xác minh",
                3 => "Công an có thẩm quyền duyệt",
                4 => "Nhân viên nhà xác",
                5 => "Người dân / người thân",
                _ => "Không xác định"
            };

        string maCodeVaiTro = user.Role?.Code ?? user.MaVaiTro switch
        {
            1 => "ADMIN",
            2 => "CONG_AN",
            3 => "CONG_AN_DUYET",
            4 => "NHA_XAC",
            5 => "NGUOI_DAN",
            _ => "OTHER"
        };

        string tenVaiTro = user.Role?.TenVaiTro ?? "KhongXacDinh";

        return new UserDto
        {
            MaTaiKhoan = user.MaTaiKhoan,
            TenDangNhap = user.TenDangNhap,
            HoTen = user.HoTen,
            Email = user.Email,
            SoDienThoai = user.SoDienThoai,
            CCCD = user.CCCD,
            DonVi = user.DonVi,
            ChucVu = user.ChucVu,
            MaVaiTro = user.MaVaiTro,
            TenVaiTro = tenVaiTro,
            MaCodeVaiTro = maCodeVaiTro,
            TrangThai = user.TrangThai,
            SoLanDangNhapSai = user.SoLanDangNhapSai,
            KhoaDenThoiDiem = user.KhoaDenThoiDiem,
            LanDangNhapCuoi = user.LanDangNhapCuoi,
            NgayTao = user.NgayTao,
            NgayCapNhat = user.NgayCapNhat
        };
    }

    // GetPagedListAsync:           AccessControl.Require(ResourceAccount, ActionView);
    // GetByIdAsync(long id):       AccessControl.RequireSelfOr(id, ResourceAccount, ActionView);   // xem hồ sơ của chính mình thì được
    // GetByUsernameAsync:          AccessControl.Require(ResourceAccount, ActionView);
    // CreateAsync:                 AccessControl.Require(ResourceAccount, ActionCreate);
    // UpdateAsync(long id, ...):   AccessControl.Require(ResourceAccount, ActionUpdate);
    // UpdateProfileAsync:          AccessControl.RequireSelfOr(currentUserId, ResourceAccount, ActionUpdate);
    // ChangeStatusAsync:           AccessControl.Require(ResourceAccount, ActionUpdate);
    // ResetPasswordAsync:          AccessControl.Require(ResourceAccount, ActionUpdate);
    // DeleteAsync:                 AccessControl.Require(ResourceAccount, ActionUpdate);
}
