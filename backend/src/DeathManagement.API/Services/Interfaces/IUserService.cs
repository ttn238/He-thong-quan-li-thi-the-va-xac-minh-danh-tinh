using DeathManagement.API.DTOs.User;

namespace DeathManagement.API.Services.Interfaces;

/// <summary>
/// Giao diện dịch vụ nghiệp vụ quản lý tài khoản người dùng
/// </summary>
public interface IUserService
{
    Task<UserDto?> GetByIdAsync(long id);
    Task<UserDto?> GetByUsernameAsync(string username);
    Task<PagedResult<UserDto>> GetPagedListAsync(UserFilterRequest filter);
    Task<UserDto> CreateAsync(CreateUserRequest request);
    Task<UserDto?> UpdateAsync(long id, UpdateUserRequest request);
    Task<UserDto?> UpdateProfileAsync(long currentUserId, UpdateProfileRequest request);
    Task<bool> ChangeStatusAsync(long adminId, long targetUserId, ChangeUserStatusRequest request);
    Task<bool> ResetPasswordAsync(long adminId, long targetUserId, ResetPasswordRequest request);
    Task<bool> DeleteAsync(long adminId, long targetUserId);
}
