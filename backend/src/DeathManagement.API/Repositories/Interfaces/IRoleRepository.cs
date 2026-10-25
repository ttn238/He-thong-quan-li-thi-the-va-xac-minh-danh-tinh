using DeathManagement.API.DTOs.Role;
using DeathManagement.API.Entities;

namespace DeathManagement.API.Repositories.Interfaces;

public interface IRoleRepository
{
    Task<List<RoleDto>> GetRolesWithStatsAsync();
    Task<RoleDto?> GetRoleDetailsAsync(int roleId);
    Task<List<PermissionDto>> GetAllPermissionsAsync();
    Task<bool> RoleExistsAsync(int roleId);
    Task<User?> GetUserWithRoleAsync(long userId);
    Task<bool> AssignRoleAsync(long userId, int roleId);
    Task<int> CountActiveAdminsAsync();
    Task<bool> UpdateRolePermissionsAsync(int roleId, List<RolePermissionItemRequest> permissions);
}
