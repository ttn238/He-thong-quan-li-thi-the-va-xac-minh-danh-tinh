using DeathManagement.API.Common;
using DeathManagement.API.DTOs.Role;
using DeathManagement.API.Entities;
using DeathManagement.API.Repositories.Interfaces;
using DeathManagement.API.Services;
using Microsoft.EntityFrameworkCore.Storage;
using System.Security.Claims;

namespace DeathManagement.Tests;

/// <summary>
/// Lớp giả lập (Mock Repository) cho việc kiểm thử đơn vị phân hệ Quản lý Role (4.5)
/// </summary>
public class MockRoleRepository : IRoleRepository
{
    private readonly List<Role> _roles = new();
    private readonly List<User> _users = new();
    private readonly List<Permission> _permissions = new();
    private readonly List<RolePermission> _rolePermissions = new();

    public MockRoleRepository()
    {
        _roles.AddRange(new[]
        {
            new Role { MaVaiTro = 1, TenVaiTro = RoleConstants.AdminName, MaCode = RoleConstants.Admin, TrangThai = true },
            new Role { MaVaiTro = 2, TenVaiTro = RoleConstants.OfficerName, MaCode = RoleConstants.Officer, TrangThai = true },
            new Role { MaVaiTro = 3, TenVaiTro = RoleConstants.ApproverName, MaCode = RoleConstants.Approver, TrangThai = true },
            new Role { MaVaiTro = 4, TenVaiTro = RoleConstants.MorgueName, MaCode = RoleConstants.Morgue, TrangThai = true },
            new Role { MaVaiTro = 5, TenVaiTro = RoleConstants.CitizenName, MaCode = RoleConstants.Citizen, TrangThai = true },
        });

        _permissions.AddRange(new[]
        {
            new Permission { MaQuyen = 1, Resource = "ACCOUNT", Action = "VIEW" },
            new Permission { MaQuyen = 2, Resource = "ACCOUNT", Action = "UPDATE" },
            new Permission { MaQuyen = 3, Resource = "PERMISSION", Action = "UPDATE" },
            new Permission { MaQuyen = 4, Resource = "BODY", Action = "VIEW" },
            new Permission { MaQuyen = 5, Resource = "VERIFICATION", Action = "APPROVE" },
        });

        _rolePermissions.Add(new RolePermission { MaVaiTro = 1, MaQuyen = 1, PhamVi = "ALL", Role = _roles[0], Permission = _permissions[0] });
        _rolePermissions.Add(new RolePermission { MaVaiTro = 1, MaQuyen = 2, PhamVi = "ALL", Role = _roles[0], Permission = _permissions[1] });
        _rolePermissions.Add(new RolePermission { MaVaiTro = 1, MaQuyen = 3, PhamVi = "ALL", Role = _roles[0], Permission = _permissions[2] });

        _users.Add(new User
        {
            MaTaiKhoan = 1,
            TenDangNhap = "admin01",
            HoTen = "Quản trị viên 1",
            MaVaiTro = 1,
            Role = _roles[0],
            TrangThai = "HoatDong"
        });
        _users.Add(new User
        {
            MaTaiKhoan = 2,
            TenDangNhap = "admin02",
            HoTen = "Quản trị viên 2",
            MaVaiTro = 1,
            Role = _roles[0],
            TrangThai = "HoatDong"
        });
        _users.Add(new User
        {
            MaTaiKhoan = 3,
            TenDangNhap = "officer01",
            HoTen = "Cán bộ công an",
            MaVaiTro = 2,
            Role = _roles[1],
            TrangThai = "HoatDong"
        });
    }

    public Task<List<RoleDto>> GetRolesWithStatsAsync()
    {
        return Task.FromResult(_roles.Select(r => new RoleDto
        {
            MaVaiTro = r.MaVaiTro,
            TenVaiTro = r.TenVaiTro,
            MaCode = r.MaCode,
            UserCount = _users.Count(u => u.MaVaiTro == r.MaVaiTro)
        }).ToList());
    }

    public Task<RoleDto?> GetRoleDetailsAsync(int roleId)
    {
        var role = _roles.FirstOrDefault(r => r.MaVaiTro == roleId);
        if (role == null) return Task.FromResult<RoleDto?>(null);

        return Task.FromResult<RoleDto?>(new RoleDto
        {
            MaVaiTro = role.MaVaiTro,
            TenVaiTro = role.TenVaiTro,
            MaCode = role.MaCode,
            Permissions = _rolePermissions.Where(rp => rp.MaVaiTro == roleId).Select(rp => new PermissionDto
            {
                MaQuyen = rp.MaQuyen,
                PhamVi = rp.PhamVi
            }).ToList()
        });
    }

    public Task<List<PermissionDto>> GetAllPermissionsAsync()
    {
        return Task.FromResult(_permissions.Select(p => new PermissionDto
        {
            MaQuyen = p.MaQuyen,
            Resource = p.Resource,
            Action = p.Action
        }).ToList());
    }

    public Task<bool> RoleExistsAsync(int roleId)
    {
        return Task.FromResult(_roles.Any(r => r.MaVaiTro == roleId && r.TrangThai));
    }

    public Task<Role?> GetRoleByIdAsync(int roleId)
    {
        return Task.FromResult(_roles.FirstOrDefault(r => r.MaVaiTro == roleId));
    }

    public Task<User?> GetUserWithRoleAsync(long userId)
    {
        return Task.FromResult(_users.FirstOrDefault(u => u.MaTaiKhoan == userId));
    }

    public Task<bool> AssignRoleAsync(long userId, int roleId)
    {
        var u = _users.FirstOrDefault(x => x.MaTaiKhoan == userId);
        if (u == null) return Task.FromResult(false);

        var r = _roles.FirstOrDefault(x => x.MaVaiTro == roleId);
        u.MaVaiTro = roleId;
        u.Role = r;
        return Task.FromResult(true);
    }

    public Task<int> CountActiveAdminsAsync()
    {
        return Task.FromResult(_users.Count(u => u.Role?.MaCode == RoleConstants.Admin && u.TrangThai == "HoatDong"));
    }

    public Task<List<int>> GetExistingPermissionIdsAsync(IEnumerable<int> permissionIds)
    {
        return Task.FromResult(_permissions.Where(p => permissionIds.Contains(p.MaQuyen)).Select(p => p.MaQuyen).ToList());
    }

    public Task<bool> UpdateRolePermissionsAsync(int roleId, List<RolePermissionItemRequest> permissions)
    {
        _rolePermissions.RemoveAll(rp => rp.MaVaiTro == roleId);
        foreach (var p in permissions)
        {
            _rolePermissions.Add(new RolePermission { MaVaiTro = roleId, MaQuyen = p.MaQuyen, PhamVi = p.PhamVi });
        }
        return Task.FromResult(true);
    }

    public Task<IDbContextTransaction> BeginTransactionAsync()
    {
        return Task.FromResult<IDbContextTransaction>(new FakeTransaction());
    }

    public Task WriteAuditLogAsync(string action, string detail, long? actorId)
    {
        return Task.CompletedTask;
    }

    private class FakeTransaction : IDbContextTransaction
    {
        public Guid TransactionId => Guid.NewGuid();
        public void Commit() { }
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Rollback() { }
        public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}

public class RoleManagementTestSuite
{
    private readonly RoleService _service;
    private readonly MockRoleRepository _repo;

    public RoleManagementTestSuite()
    {
        _repo = new MockRoleRepository();
        _service = new RoleService(_repo);
    }

    // 1. Valid role assignment succeeds
    [Fact]
    public async Task AssignRole_ValidRequest_Succeeds()
    {
        var req = new AssignRoleRequest { MaTaiKhoan = 3, MaVaiTro = 3 }; // Gán CONG_AN_DUYET cho officer01
        var result = await _service.AssignRoleAsync(1, req);
        Assert.True(result);
    }

    // 2. Non-existent role is rejected
    [Fact]
    public async Task AssignRole_NonExistentRole_ThrowsKeyNotFoundException()
    {
        var req = new AssignRoleRequest { MaTaiKhoan = 3, MaVaiTro = 99 };
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.AssignRoleAsync(1, req));
    }

    // 3. Non-existent user is rejected
    [Fact]
    public async Task AssignRole_NonExistentUser_ThrowsKeyNotFoundException()
    {
        var req = new AssignRoleRequest { MaTaiKhoan = 9999, MaVaiTro = 2 };
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.AssignRoleAsync(1, req));
    }

    // 4. Self-role change is rejected
    [Fact]
    public async Task AssignRole_SelfRoleChange_ThrowsInvalidOperationException()
    {
        var req = new AssignRoleRequest { MaTaiKhoan = 1, MaVaiTro = 2 };
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.AssignRoleAsync(1, req));
    }

    // 5. Final administrator removal is rejected
    [Fact]
    public async Task AssignRole_FinalAdminRemoval_ThrowsInvalidOperationException()
    {
        // Hạ quyền admin02 trước -> còn đúng 1 admin (admin01)
        await _service.AssignRoleAsync(1, new AssignRoleRequest { MaTaiKhoan = 2, MaVaiTro = 2 });

        // Giờ admin02 thử hạ quyền admin01 -> Bị chặn
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.AssignRoleAsync(2, new AssignRoleRequest { MaTaiKhoan = 1, MaVaiTro = 2 }));
    }

    // 6. Another administrator can change a different administrator when at least two active administrators exist
    [Fact]
    public async Task AssignRole_WhenTwoAdminsExist_OneCanChangeOtherAdmin()
    {
        var req = new AssignRoleRequest { MaTaiKhoan = 2, MaVaiTro = 2 };
        var result = await _service.AssignRoleAsync(1, req); // admin01 đổi vai trò admin02
        Assert.True(result);
    }

    // 7. Invalid permission ID is rejected
    [Fact]
    public async Task UpdatePermissions_InvalidPermissionId_ThrowsKeyNotFoundException()
    {
        var req = new UpdateRolePermissionsRequest
        {
            Permissions = new List<RolePermissionItemRequest>
            {
                new() { MaQuyen = 999, PhamVi = "ALL" }
            }
        };
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.UpdateRolePermissionsAsync(1, 2, req));
    }

    // 8. Duplicate permission ID is rejected
    [Fact]
    public async Task UpdatePermissions_DuplicatePermissionId_ThrowsInvalidOperationException()
    {
        var req = new UpdateRolePermissionsRequest
        {
            Permissions = new List<RolePermissionItemRequest>
            {
                new() { MaQuyen = 1, PhamVi = "ALL" },
                new() { MaQuyen = 1, PhamVi = "OWN" }
            }
        };
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateRolePermissionsAsync(1, 2, req));
    }

    // 9. Invalid scope is rejected
    [Fact]
    public async Task UpdatePermissions_InvalidScope_ThrowsInvalidOperationException()
    {
        var req = new UpdateRolePermissionsRequest
        {
            Permissions = new List<RolePermissionItemRequest>
            {
                new() { MaQuyen = 1, PhamVi = "INVALID_SCOPE" }
            }
        };
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateRolePermissionsAsync(1, 2, req));
    }

    // 10. Admin role core permission protection
    [Fact]
    public async Task UpdatePermissions_AdminMissingCorePerms_ThrowsInvalidOperationException()
    {
        var req = new UpdateRolePermissionsRequest
        {
            Permissions = new List<RolePermissionItemRequest>
            {
                new() { MaQuyen = 4, PhamVi = "ALL" } // Chỉ cấp BODY:VIEW, thiếu ACCOUNT:VIEW và PERMISSION:UPDATE
            }
        };
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateRolePermissionsAsync(1, 1, req));
    }

    }
