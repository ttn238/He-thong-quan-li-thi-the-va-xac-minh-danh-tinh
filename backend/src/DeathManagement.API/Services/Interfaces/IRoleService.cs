using DeathManagement.API.DTOs.Role;

namespace DeathManagement.API.Services.Interfaces;

public interface IRoleService
{
    Task<List<RoleDto>> GetRolesWithStatsAsync();
    Task<RoleDto?> GetRoleDetailsAsync(int roleId);
    Task<List<ResourceGroupedPermissionsDto>> GetAllPermissionsGroupedAsync();
    Task<bool> AssignRoleAsync(long currentUserId, AssignRoleRequest request);
    Task<bool> UpdateRolePermissionsAsync(long currentUserId, int roleId, UpdateRolePermissionsRequest request);
}
