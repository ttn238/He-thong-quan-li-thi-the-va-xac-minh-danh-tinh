using DeathManagement.API.Common;
using DeathManagement.API.DTOs.Role;
using DeathManagement.API.Entities;
using DeathManagement.API.Repositories.Interfaces;
using DeathManagement.API.Services;

namespace DeathManagement.Tests;

/// <summary>
/// Lớp giả lập (Mock Repository) cho việc kiểm thử đơn vị phân hệ Quản lý Role (4.5)
/// Không phụ thuộc trực tiếp vào kết nối mạng CSDL vật lý.
/// </summary>
public class MockRoleRepository : IRoleRepository
{
    private readonly List<Role> _roles = new();
    private readonly List<User> _users = new();
    private readonly List<Permission> _permissions = new();
    private readonly List<RolePermission> _rolePermissions = new();

    public MockRoleRepository()
    {
        // Khởi tạo 5 vai trò hệ thống
        _roles.AddRange(new[]
        {
            new Role { MaVaiTro = RoleConstants.AdminId, TenVaiTro = RoleConstants.AdminName, MaCode = RoleConstants.Admin },
            new Role { MaVaiTro = RoleConstants.OfficerId, TenVaiTro = RoleConstants.OfficerName, MaCode = RoleConstants.Officer },
            new Role { MaVaiTro = RoleConstants.ApproverId, TenVaiTro = RoleConstants.ApproverName, MaCode = RoleConstants.Approver },
            new Role { MaVaiTro = RoleConstants.MorgueId, TenVaiTro = RoleConstants.MorgueName, MaCode = RoleConstants.Morgue },
            new Role { MaVaiTro = RoleConstants.CitizenId, TenVaiTro = RoleConstants.CitizenName, MaCode = RoleConstants.Citizen },
        });

        // Khởi tạo một số quyền mẫu
        _permissions.AddRange(new[]
        {
            new Permission { MaQuyen = 1, Resource = "ACCOUNT", Action = "VIEW" },
            new Permission { MaQuyen = 2, Resource = "ACCOUNT", Action = "UPDATE" },
            new Permission { MaQuyen = 3, Resource = "PERMISSION", Action = "UPDATE" },
            new Permission { MaQuyen = 4, Resource = "BODY", Action = "VIEW" },
            new Permission { MaQuyen = 5, Resource = "VERIFICATION", Action = "APPROVE" },
        });

        // Phân quyền cho ADMIN
        _rolePermissions.Add(new RolePermission { MaVaiTro = 1, MaQuyen = 1, PhamVi = "ALL", Role = _roles[0], Permission = _permissions[0] });
        _rolePermissions.Add(new RolePermission { MaVaiTro = 1, MaQuyen = 2, PhamVi = "ALL", Role = _roles[0], Permission = _permissions[1] });
        _rolePermissions.Add(new RolePermission { MaVaiTro = 1, MaQuyen = 3, PhamVi = "ALL", Role = _roles[0], Permission = _permissions[2] });

        // Người dùng mẫu
        _users.Add(new User
        {
            MaTaiKhoan = 1,
            TenDangNhap = "admin",
            HoTen = "Quản trị viên Hệ thống",
            MaVaiTro = RoleConstants.AdminId,
            TrangThai = "HoatDong"
        });

        _users.Add(new User
        {
            MaTaiKhoan = 2,
            TenDangNhap = "canbo01",
            HoTen = "Nguyễn Văn An",
            MaVaiTro = RoleConstants.OfficerId,
            TrangThai = "HoatDong"
        });
    }

    public Task<List<RoleDto>> GetRolesWithStatsAsync()
    {
        var result = _roles.Select(r => new RoleDto
        {
            MaVaiTro = r.MaVaiTro,
            TenVaiTro = r.TenVaiTro,
            MaCode = RoleConstants.GetCode(r.MaVaiTro),
            UserCount = _users.Count(u => u.MaVaiTro == r.MaVaiTro)
        }).ToList();

        return Task.FromResult(result);
    }

    public Task<RoleDto?> GetRoleDetailsAsync(int roleId)
    {
        var role = _roles.FirstOrDefault(r => r.MaVaiTro == roleId);
        if (role == null) return Task.FromResult<RoleDto?>(null);

        var rps = _rolePermissions.Where(rp => rp.MaVaiTro == roleId).ToList();
        var dto = new RoleDto
        {
            MaVaiTro = role.MaVaiTro,
            TenVaiTro = role.TenVaiTro,
            MaCode = RoleConstants.GetCode(role.MaVaiTro),
            UserCount = _users.Count(u => u.MaVaiTro == role.MaVaiTro),
            Permissions = rps.Select(rp => new PermissionDto
            {
                MaQuyen = rp.MaQuyen,
                Resource = rp.Permission?.Resource ?? string.Empty,
                Action = rp.Permission?.Action ?? string.Empty,
                TenQuyen = $"{rp.Permission?.Resource}:{rp.Permission?.Action}",
                PhamVi = rp.PhamVi
            }).ToList()
        };

        return Task.FromResult<RoleDto?>(dto);
    }

    public Task<List<PermissionDto>> GetAllPermissionsAsync()
    {
        var result = _permissions.Select(p => new PermissionDto
        {
            MaQuyen = p.MaQuyen,
            Resource = p.Resource,
            Action = p.Action,
            TenQuyen = $"{p.Resource}:{p.Action}"
        }).ToList();

        return Task.FromResult(result);
    }

    public Task<bool> RoleExistsAsync(int roleId)
    {
        return Task.FromResult(_roles.Any(r => r.MaVaiTro == roleId));
    }

    public Task<User?> GetUserWithRoleAsync(long userId)
    {
        var u = _users.FirstOrDefault(x => x.MaTaiKhoan == userId);
        return Task.FromResult(u);
    }

    public Task<bool> AssignRoleAsync(long userId, int roleId)
    {
        var u = _users.FirstOrDefault(x => x.MaTaiKhoan == userId);
        if (u == null) return Task.FromResult(false);

        u.MaVaiTro = roleId;
        return Task.FromResult(true);
    }

    public Task<int> CountActiveAdminsAsync()
    {
        return Task.FromResult(_users.Count(u => u.MaVaiTro == RoleConstants.AdminId && u.TrangThai == "HoatDong"));
    }

    public Task<bool> UpdateRolePermissionsAsync(int roleId, List<RolePermissionItemRequest> permissions)
    {
        _rolePermissions.RemoveAll(rp => rp.MaVaiTro == roleId);
        foreach (var p in permissions)
        {
            var perm = _permissions.FirstOrDefault(x => x.MaQuyen == p.MaQuyen);
            _rolePermissions.Add(new RolePermission
            {
                MaVaiTro = roleId,
                MaQuyen = p.MaQuyen,
                PhamVi = p.PhamVi,
                Permission = perm!
            });
        }
        return Task.FromResult(true);
    }

    public void AddAdmin(User admin) => _users.Add(admin);
}

/// <summary>
/// Bộ kiểm thử đơn vị cho Phân hệ 4.5: Quản lý Role & RBAC Permissions.
/// </summary>
public class RoleManagementTests
{
    private readonly MockRoleRepository _repo;
    private readonly RoleService _service;

    public RoleManagementTests()
    {
        _repo = new MockRoleRepository();
        _service = new RoleService(_repo);
    }

    [Fact]
    public async Task GetRoles_ReturnsAllFiveRoles_WithCorrectUserCounts()
    {
        // 1. Kiểm tra lấy đủ 5 vai trò hệ thống
        var roles = await _service.GetRolesWithStatsAsync();

        Assert.Equal(5, roles.Count);
        Assert.Contains(roles, r => r.MaCode == "ADMIN" && r.UserCount == 1);
        Assert.Contains(roles, r => r.MaCode == "CONG_AN" && r.UserCount == 1);
        Assert.Contains(roles, r => r.MaCode == "CONG_AN_DUYET" && r.UserCount == 0);
        Assert.Contains(roles, r => r.MaCode == "NHA_XAC" && r.UserCount == 0);
        Assert.Contains(roles, r => r.MaCode == "NGUOI_DAN" && r.UserCount == 0);
    }

    [Fact]
    public async Task GetRoleDetails_ValidId_ReturnsRoleWithPermissions()
    {
        // 2. Kiểm tra xem chi tiết quyền hạn của vai trò ADMIN
        var role = await _service.GetRoleDetailsAsync(RoleConstants.AdminId);

        Assert.NotNull(role);
        Assert.Equal(RoleConstants.AdminName, role.TenVaiTro);
        Assert.Equal(3, role.Permissions.Count);
        Assert.Contains(role.Permissions, p => p.Resource == "ACCOUNT" && p.Action == "VIEW");
    }

    [Fact]
    public async Task AssignRole_ValidRole_UpdatesUserSuccessfully()
    {
        // 3. Gán vai trò hợp lệ: Cán bộ #2 sang Nhà xác (NHA_XAC)
        var result = await _service.AssignRoleAsync(1, new AssignRoleRequest
        {
            MaTaiKhoan = 2,
            MaVaiTro = RoleConstants.MorgueId
        });

        Assert.True(result);
        var updated = await _repo.GetUserWithRoleAsync(2);
        Assert.Equal(RoleConstants.MorgueId, updated!.MaVaiTro);
    }

    [Fact]
    public async Task AssignRole_NonExistentRole_ThrowsKeyNotFoundException()
    {
        // 4. Báo lỗi khi gán mã vai trò không tồn tại (ví dụ 999)
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.AssignRoleAsync(1, new AssignRoleRequest
            {
                MaTaiKhoan = 2,
                MaVaiTro = 999
            }));

        Assert.Contains("không tồn tại", ex.Message);
    }

    [Fact]
    public async Task AssignRole_NonExistentUser_ThrowsKeyNotFoundException()
    {
        // 5. Báo lỗi khi gán vai trò cho tài khoản không tồn tại
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.AssignRoleAsync(1, new AssignRoleRequest
            {
                MaTaiKhoan = 8888,
                MaVaiTro = RoleConstants.OfficerId
            }));

        Assert.Contains("Không tìm thấy tài khoản", ex.Message);
    }

    [Fact]
    public async Task AssignRole_AdminSelfDemotion_ThrowsInvalidOperationException()
    {
        // 6. Chống tự hạ quyền: Admin #1 tự đổi vai trò của chính mình thành Người dân
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.AssignRoleAsync(1, new AssignRoleRequest
            {
                MaTaiKhoan = 1,
                MaVaiTro = RoleConstants.CitizenId
            }));

        Assert.Contains("tự hạ quyền", ex.Message);
    }

    [Fact]
    public async Task AssignRole_LastAdminDemotion_ThrowsInvalidOperationException()
    {
        // 7. Bảo vệ Quản trị viên duy nhất: Khi chỉ có 1 Admin duy nhất, không thể chuyển admin đó sang vai trò khác
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.AssignRoleAsync(99, new AssignRoleRequest // Giả sử người gọi khác
            {
                MaTaiKhoan = 1,
                MaVaiTro = RoleConstants.OfficerId
            }));

        Assert.Contains("Quản trị viên duy nhất", ex.Message);
    }

    [Fact]
    public async Task AssignRole_WhenMultipleAdmins_AllowsRoleChangeForOtherAdmin()
    {
        // 8. Khi có nhiều hơn 1 Admin đang hoạt động, cho phép đổi vai trò admin khác
        _repo.AddAdmin(new User
        {
            MaTaiKhoan = 3,
            TenDangNhap = "admin2",
            HoTen = "Quản trị viên số 2",
            MaVaiTro = RoleConstants.AdminId,
            TrangThai = "HoatDong"
        });

        var result = await _service.AssignRoleAsync(1, new AssignRoleRequest
        {
            MaTaiKhoan = 3,
            MaVaiTro = RoleConstants.ApproverId
        });

        Assert.True(result);
        var target = await _repo.GetUserWithRoleAsync(3);
        Assert.Equal(RoleConstants.ApproverId, target!.MaVaiTro);
    }

    [Fact]
    public async Task UpdateRolePermissions_ValidRole_UpdatesSuccessfully()
    {
        // 9. Cập nhật ma trận phân quyền thành công
        var result = await _service.UpdateRolePermissionsAsync(1, RoleConstants.OfficerId, new UpdateRolePermissionsRequest
        {
            Permissions = new List<RolePermissionItemRequest>
            {
                new() { MaQuyen = 4, PhamVi = "ALL" },
                new() { MaQuyen = 5, PhamVi = "OWN" }
            }
        });

        Assert.True(result);
        var details = await _service.GetRoleDetailsAsync(RoleConstants.OfficerId);
        Assert.Equal(2, details!.Permissions.Count);
    }
}
