using DeathManagement.API.Common;
using DeathManagement.API.DTOs.Role;
using DeathManagement.API.Repositories;
using DeathManagement.API.Repositories.Interfaces;
using DeathManagement.API.Services.Interfaces;

namespace DeathManagement.API.Services;

/// <summary>
/// Dịch vụ nghiệp vụ quản trị vai trò, phân quyền và kiểm soát ma trận RBAC.
/// Thực thi nghiêm ngặt các quy tắc an ninh:
/// 1. Chống tự leo thang đặc quyền (Anti Privilege Escalation).
/// 2. Bảo vệ tài khoản Quản trị viên cuối cùng (Last Admin Protection).
/// 3. Kiểm tra tính tồn tại của vai trò trong CSDL.
/// </summary>
public class RoleService : IRoleService
{
    private readonly IRoleRepository _roleRepo;

    public RoleService(IRoleRepository? roleRepo = null)
    {
        _roleRepo = roleRepo ?? new RoleRepository();
    }

    /// <summary>
    /// Lấy danh sách 5 vai trò hệ thống kèm thống kê số lượng tài khoản thuộc từng vai trò.
    /// </summary>
    public async Task<List<RoleDto>> GetRolesWithStatsAsync()
    {
        return await _roleRepo.GetRolesWithStatsAsync();
    }

    /// <summary>
    /// Lấy thông tin chi tiết một vai trò kèm danh sách quyền hạn (QuyenHeThong) và phạm vi dữ liệu.
    /// </summary>
    public async Task<RoleDto?> GetRoleDetailsAsync(int roleId)
    {
        return await _roleRepo.GetRoleDetailsAsync(roleId);
    }

    /// <summary>
    /// Lấy toàn bộ 48 quyền hệ thống phân chia theo nhóm tài nguyên (Resource).
    /// </summary>
    public async Task<List<ResourceGroupedPermissionsDto>> GetAllPermissionsGroupedAsync()
    {
        var all = await _roleRepo.GetAllPermissionsAsync();
        return all
            .GroupBy(p => p.Resource)
            .Select(g => new ResourceGroupedPermissionsDto
            {
                Resource = g.Key,
                Permissions = g.ToList()
            })
            .OrderBy(g => g.Resource)
            .ToList();
    }

    /// <summary>
    /// Gán hoặc thay đổi vai trò cho một tài khoản người dùng với các quy tắc kiểm tra bảo mật.
    /// </summary>
    public async Task<bool> AssignRoleAsync(long currentUserId, AssignRoleRequest request)
    {
        // Bước 1: Kiểm tra vai trò đích có tồn tại trong hệ thống hay không
        var roleExists = await _roleRepo.RoleExistsAsync(request.MaVaiTro);
        if (!roleExists)
        {
            throw new KeyNotFoundException($"Vai trò với mã #{request.MaVaiTro} không tồn tại trong hệ thống.");
        }

        // Bước 2: Kiểm tra tài khoản đích có tồn tại hay không
        var targetUser = await _roleRepo.GetUserWithRoleAsync(request.MaTaiKhoan);
        if (targetUser == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy tài khoản với mã #{request.MaTaiKhoan}.");
        }

        // Bước 3: Quy tắc an ninh - Chống tự hạ quyền của chính mình
        // Quản trị viên đang đăng nhập không thể tự đổi vai trò của mình thành vai trò thường (tránh tự khóa mất quyền quản trị)
        if (currentUserId > 0 && currentUserId == targetUser.MaTaiKhoan && targetUser.MaVaiTro == RoleConstants.AdminId && request.MaVaiTro != RoleConstants.AdminId)
        {
            throw new InvalidOperationException("Quy tắc an toàn: Bạn không thể tự hạ quyền quản trị viên của chính mình.");
        }

        // Bước 4: Quy tắc an ninh - Bảo vệ Quản trị viên duy nhất (Last Admin Protection)
        // Không cho phép đổi vai trò tài khoản Admin nếu đây là tài khoản Quản trị viên duy nhất đang hoạt động
        if (targetUser.MaVaiTro == RoleConstants.AdminId && request.MaVaiTro != RoleConstants.AdminId)
        {
            var activeAdminCount = await _roleRepo.CountActiveAdminsAsync();
            if (activeAdminCount <= 1)
            {
                throw new InvalidOperationException("Không thể thay đổi vai trò của tài khoản Quản trị viên duy nhất đang hoạt động trong hệ thống.");
            }
        }

        // Bước 5: Cập nhật vai trò mới vào CSDL
        return await _roleRepo.AssignRoleAsync(targetUser.MaTaiKhoan, request.MaVaiTro);
    }

    /// <summary>
    /// Cập nhật ma trận phân quyền (danh sách quyền và phạm vi) cho một vai trò.
    /// </summary>
    public async Task<bool> UpdateRolePermissionsAsync(long currentUserId, int roleId, UpdateRolePermissionsRequest request)
    {
        var roleExists = await _roleRepo.RoleExistsAsync(roleId);
        if (!roleExists)
        {
            throw new KeyNotFoundException($"Vai trò với mã #{roleId} không tồn tại.");
        }

        return await _roleRepo.UpdateRolePermissionsAsync(roleId, request.Permissions);
    }
}
