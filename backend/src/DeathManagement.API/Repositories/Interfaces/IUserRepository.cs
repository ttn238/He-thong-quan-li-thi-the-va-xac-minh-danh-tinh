using DeathManagement.API.DTOs.User;
using DeathManagement.API.Entities;

namespace DeathManagement.API.Repositories.Interfaces;

/// <summary>
/// Giao diện kho lưu trữ dữ liệu tài khoản người dùng
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(long id);
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
    Task<PagedResult<User>> GetPagedListAsync(UserFilterRequest filter);
    Task<User> CreateAsync(User user);
    Task<User> UpdateAsync(User user);
    Task<bool> UpdateStatusAsync(long id, string status);
    Task<bool> UpdatePasswordAsync(long id, string newPasswordHash);
    Task<bool> DeleteAsync(long id);

    Task<bool> ExistsByUsernameAsync(string username, long? excludeId = null);
    Task<bool> ExistsByEmailAsync(string email, long? excludeId = null);
    Task<bool> ExistsByPhoneAsync(string phone, long? excludeId = null);
    Task<bool> ExistsByCccdAsync(string cccd, long? excludeId = null);
}
