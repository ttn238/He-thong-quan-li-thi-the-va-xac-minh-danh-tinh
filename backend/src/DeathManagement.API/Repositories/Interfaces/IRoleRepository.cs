using DeathManagement.API.DTOs.Role;
using DeathManagement.API.Entities;
using Microsoft.EntityFrameworkCore.Storage;

namespace DeathManagement.API.Repositories.Interfaces;

public interface IRoleRepository
{
    Task<List<RoleDto>> GetRolesWithStatsAsync();
    Task<RoleDto?> GetRoleDetailsAsync(int roleId);
    Task<List<PermissionDto>> GetAllPermissionsAsync();
    Task<bool> RoleExistsAsync(int roleId);
    Task<Role?> GetRoleByIdAsync(int roleId);
    Task<User?> GetUserWithRoleAsync(long userId);
    Task<bool> AssignRoleAsync(long userId, int roleId);
    Task<int> CountActiveAdminsAsync();
    Task<List<int>> GetExistingPermissionIdsAsync(IEnumerable<int> permissionIds);
    Task<bool> UpdateRolePermissionsAsync(int roleId, List<RolePermissionItemRequest> permissions);
    Task<IDbContextTransaction> BeginTransactionAsync();
    Task WriteAuditLogAsync(string action, string detail, long? actorId);
}
