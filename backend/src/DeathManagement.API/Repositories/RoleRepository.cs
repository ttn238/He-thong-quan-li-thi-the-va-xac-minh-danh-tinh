using DeathManagement.API.Common;
using DeathManagement.API.Data;
using DeathManagement.API.DTOs.Role;
using DeathManagement.API.Entities;
using DeathManagement.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DeathManagement.API.Repositories;

/// <summary>
/// Kho lưu trữ dữ liệu Vai trò và Quyền hạn tương tác trực tiếp với CSDL QL_XACMINHTHITHE qua EF Core.
/// </summary>
public class RoleRepository : IRoleRepository
{
    private readonly ApplicationDbContext _db;

    public RoleRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Lấy danh sách toàn bộ các vai trò và đếm số người dùng đang giữ vai trò đó.
    /// </summary>
    public async Task<List<RoleDto>> GetRolesWithStatsAsync()
    {
        var roles = await _db.Roles
            .AsNoTracking()
            .Include(r => r.Users)
            .OrderBy(r => r.MaVaiTro)
            .ToListAsync();

        return roles.Select(r => new RoleDto
        {
            MaVaiTro = r.MaVaiTro,
            TenVaiTro = r.TenVaiTro,
            MaCode = !string.IsNullOrEmpty(r.MaCode) ? r.MaCode : RoleConstants.GetCode(r.MaVaiTro),
            MoTa = !string.IsNullOrEmpty(r.MoTa) ? r.MoTa : GetRoleDescription(r.MaCode),
            UserCount = r.Users.Count
        }).ToList();
    }

    /// <summary>
    /// Lấy chi tiết một vai trò kèm danh sách quyền hạn đã gán trong bảng VaiTro_Quyen.
    /// </summary>
    public async Task<RoleDto?> GetRoleDetailsAsync(int roleId)
    {
        var role = await _db.Roles
            .AsNoTracking()
            .Include(r => r.Users)
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.MaVaiTro == roleId);

        if (role == null) return null;

        var code = !string.IsNullOrEmpty(role.MaCode) ? role.MaCode : RoleConstants.GetCode(role.MaVaiTro);

        return new RoleDto
        {
            MaVaiTro = role.MaVaiTro,
            TenVaiTro = role.TenVaiTro,
            MaCode = code,
            MoTa = !string.IsNullOrEmpty(role.MoTa) ? role.MoTa : GetRoleDescription(code),
            UserCount = role.Users.Count,
            Permissions = role.RolePermissions.Select(rp => new PermissionDto
            {
                MaQuyen = rp.MaQuyen,
                Resource = rp.Permission?.Resource ?? string.Empty,
                Action = rp.Permission?.Action ?? string.Empty,
                TenQuyen = rp.Permission?.TenQuyen ?? $"{rp.Permission?.Resource}:{rp.Permission?.Action}",
                PhamVi = rp.PhamVi
            }).ToList()
        };
    }

    public async Task<List<PermissionDto>> GetAllPermissionsAsync()
    {
        var permissions = await _db.Permissions
            .AsNoTracking()
            .OrderBy(p => p.Resource)
            .ThenBy(p => p.Action)
            .ToListAsync();

        return permissions.Select(p => new PermissionDto
        {
            MaQuyen = p.MaQuyen,
            Resource = p.Resource,
            Action = p.Action,
            TenQuyen = p.TenQuyen
        }).ToList();
    }

    public async Task<bool> RoleExistsAsync(int roleId)
    {
        return await _db.Roles.AnyAsync(r => r.MaVaiTro == roleId && r.TrangThai);
    }

    public async Task<Role?> GetRoleByIdAsync(int roleId)
    {
        return await _db.Roles.FirstOrDefaultAsync(r => r.MaVaiTro == roleId);
    }

    public async Task<User?> GetUserWithRoleAsync(long userId)
    {
        return await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.MaTaiKhoan == userId);
    }

    public async Task<bool> AssignRoleAsync(long userId, int roleId)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.MaTaiKhoan == userId);
        if (user == null) return false;

        user.MaVaiTro = roleId;
        user.NgayCapNhat = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<int> CountActiveAdminsAsync()
    {
        return await _db.Users
            .Include(u => u.Role)
            .CountAsync(u => (u.Role != null && u.Role.MaCode == RoleConstants.Admin) && u.TrangThai == "HoatDong");
    }

    public async Task<List<int>> GetExistingPermissionIdsAsync(IEnumerable<int> permissionIds)
    {
        return await _db.Permissions
            .Where(p => permissionIds.Contains(p.MaQuyen))
            .Select(p => p.MaQuyen)
            .ToListAsync();
    }

    public async Task<bool> UpdateRolePermissionsAsync(int roleId, List<RolePermissionItemRequest> permissions)
    {
        var existing = await _db.RolePermissions.Where(rp => rp.MaVaiTro == roleId).ToListAsync();
        _db.RolePermissions.RemoveRange(existing);

        foreach (var item in permissions)
        {
            _db.RolePermissions.Add(new RolePermission
            {
                MaVaiTro = roleId,
                MaQuyen = item.MaQuyen,
                PhamVi = item.PhamVi
            });
        }

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        return await _db.Database.BeginTransactionAsync();
    }

    public async Task WriteAuditLogAsync(string action, string detail, long? actorId)
    {
        try
        {
            var truncated = detail.Length > 255 ? detail[..255] : detail;
            await _db.Database.ExecuteSqlInterpolatedAsync($"EXEC sp_GhiNhatKy {action}, {truncated}, {actorId}");
        }
        catch
        {
            // Tránh ngắt quãng luồng chính nếu thủ tục ghi log không khả dụng trong môi trường test
        }
    }

    private static string GetRoleDescription(string? code) => code switch
    {
        RoleConstants.Admin => "Toàn quyền quản trị hệ thống, tài khoản và phân quyền người dùng",
        RoleConstants.Officer => "Tiếp nhận hồ sơ, tìm kiếm tử thi, đối soát ADN và điều tra thông tin",
        RoleConstants.Approver => "Thẩm quyền phê duyệt chính thức kết luận định danh và bàn giao",
        RoleConstants.Morgue => "Quản lý vị trí lưu giữ tử thi, tiếp nhận bảo quản và bàn giao",
        RoleConstants.Citizen => "Khai báo người thân mất tích và theo dõi hồ sơ cá nhân",
        _ => "Vai trò hệ thống"
    };
}
