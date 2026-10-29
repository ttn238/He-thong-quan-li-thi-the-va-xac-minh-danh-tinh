using DeathManagement.API.Common;
using DeathManagement.API.Data;
using DeathManagement.API.DTOs.Role;
using DeathManagement.API.Entities;
using DeathManagement.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DeathManagement.API.Repositories;

/// <summary>
/// Kho lưu trữ dữ liệu Vai trò và Quyền hạn tương tác trực tiếp với CSDL QL_XACMINHTHITHE qua EF Core.
/// </summary>
public class RoleRepository : IRoleRepository
{
    private readonly ApplicationDbContext _db;

    public RoleRepository(ApplicationDbContext? db = null)
    {
        _db = db ?? Program.NewDb();
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
            MaCode = RoleConstants.GetCode(r.MaVaiTro),
            MoTa = GetRoleDescription(r.MaVaiTro),
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

        return new RoleDto
        {
            MaVaiTro = role.MaVaiTro,
            TenVaiTro = role.TenVaiTro,
            MaCode = RoleConstants.GetCode(role.MaVaiTro),
            MoTa = GetRoleDescription(role.MaVaiTro),
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

    /// <summary>
    /// Lấy danh mục toàn bộ quyền hệ thống trong bảng QuyenHeThong.
    /// </summary>
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

    /// <summary>
    /// Kiểm tra vai trò có tồn tại trong CSDL hay không.
    /// </summary>
    public async Task<bool> RoleExistsAsync(int roleId)
    {
        return await _db.Roles.AnyAsync(r => r.MaVaiTro == roleId);
    }

    /// <summary>
    /// Lấy thông tin tài khoản kèm thông tin vai trò hiện tại.
    /// </summary>
    public async Task<User?> GetUserWithRoleAsync(long userId)
    {
        return await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.MaTaiKhoan == userId);
    }

    /// <summary>
    /// Cập nhật vai trò mới cho tài khoản.
    /// </summary>
    public async Task<bool> AssignRoleAsync(long userId, int roleId)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.MaTaiKhoan == userId);
        if (user == null) return false;

        user.MaVaiTro = roleId;
        user.NgayCapNhat = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Đếm số lượng tài khoản Quản trị viên (ADMIN) đang ở trạng thái HoatDong.
    /// </summary>
    public async Task<int> CountActiveAdminsAsync()
    {
        return await _db.Users
            .CountAsync(u => u.MaVaiTro == RoleConstants.AdminId && u.TrangThai == "HoatDong");
    }

    /// <summary>
    /// Cập nhật ma trận quyền hạn cho một vai trò.
    /// </summary>
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

    /// <summary>
    /// Mô tả chi tiết chức năng và thẩm quyền cho từng vai trò.
    /// </summary>
    private static string GetRoleDescription(int roleId) => roleId switch
    {
        RoleConstants.AdminId => "Toàn quyền quản trị hệ thống, tài khoản và phân quyền người dùng",
        RoleConstants.OfficerId => "Tiếp nhận hồ sơ, tìm kiếm tử thi, đối soát ADN và điều tra thông tin",
        RoleConstants.ApproverId => "Thẩm quyền phê duyệt chính thức kết luận định danh và bàn giao",
        RoleConstants.MorgueId => "Quản lý vị trí lưu giữ tử thi, tiếp nhận bảo quản và bàn giao",
        RoleConstants.CitizenId => "Khai báo người thân mất tích và theo dõi hồ sơ cá nhân",
        _ => "Vai trò hệ thống"
    };
}
