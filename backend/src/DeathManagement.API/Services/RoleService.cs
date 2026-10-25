using DeathManagement.API.Common;
using DeathManagement.API.DTOs.Role;
using DeathManagement.API.Repositories.Interfaces;
using DeathManagement.API.Services.Interfaces;

namespace DeathManagement.API.Services;

/// <summary>
/// Dịch vụ nghiệp vụ quản trị vai trò, phân quyền và kiểm soát ma trận RBAC.
/// Thực thi nghiêm ngặt các quy tắc an ninh cấp máy chủ (Server Validation):
/// 1. Yêu cầu ID người thực hiện hợp lệ (Actor validation).
/// 2. Chống tự thay đổi vai trò của chính mình (Self role-change rejection).
/// 3. Bảo vệ tài khoản Quản trị viên cuối cùng (Last Active Admin Protection).
/// 4. Kiểm tra tính tồn tại và kích hoạt của vai trò trong CSDL.
/// 5. Cập nhật quyền hạn nguyên tử qua Transaction và kiểm tra phạm vi dữ liệu hợp lệ (ALL, OWN, PARTIAL).
/// 6. Ghi Audit Log qua sp_GhiNhatKy.
/// </summary>
public class RoleService : IRoleService
{
    private readonly IRoleRepository _roleRepo;

    public RoleService(IRoleRepository roleRepo)
    {
        _roleRepo = roleRepo;
    }

    public async Task<List<RoleDto>> GetRolesWithStatsAsync()
    {
        return await _roleRepo.GetRolesWithStatsAsync();
    }

    public async Task<RoleDto?> GetRoleDetailsAsync(int roleId)
    {
        return await _roleRepo.GetRoleDetailsAsync(roleId);
    }

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
    /// Gán vai trò cho tài khoản với đầy đủ kiểm tra bảo mật phía server.
    /// </summary>
    public async Task<bool> AssignRoleAsync(long currentUserId, AssignRoleRequest request)
    {
        if (currentUserId <= 0)
        {
            throw new UnauthorizedAccessException("Không xác định được danh tính người thực hiện từ token đăng nhập.");
        }

        // 1. Kiểm tra tài khoản đích
        var targetUser = await _roleRepo.GetUserWithRoleAsync(request.MaTaiKhoan);
        if (targetUser == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy tài khoản với mã #{request.MaTaiKhoan}.");
        }

        // 2. Kiểm tra vai trò đích
        var targetRole = await _roleRepo.GetRoleByIdAsync(request.MaVaiTro);
        if (targetRole == null || !targetRole.TrangThai)
        {
            throw new KeyNotFoundException($"Vai trò với mã #{request.MaVaiTro} không tồn tại hoặc đã bị vô hiệu hóa.");
        }

        // Nếu gán lại đúng vai trò hiện tại -> trả về thành công an toàn không tác dụng phụ
        if (targetUser.MaVaiTro == request.MaVaiTro)
        {
            return true;
        }

        // 3. Quy tắc an ninh: Không cho phép người dùng tự đổi vai trò của chính mình
        if (currentUserId == targetUser.MaTaiKhoan)
        {
            throw new InvalidOperationException("Quy tắc an toàn: Bạn không được phép tự thay đổi vai trò của chính mình.");
        }

        var oldRoleCode = targetUser.Role?.MaCode ?? targetUser.Role?.Code ?? "";
        var newRoleCode = targetRole.MaCode;

        // 4. Quy tắc an ninh: Bảo vệ Quản trị viên cuối cùng (Last Active Admin Protection)
        if (RoleConstants.IsAdmin(oldRoleCode) && !RoleConstants.IsAdmin(newRoleCode))
        {
            var activeAdminCount = await _roleRepo.CountActiveAdminsAsync();
            if (activeAdminCount <= 1)
            {
                throw new InvalidOperationException("Không thể hạ quyền hoặc thay đổi vai trò của Quản trị viên đang hoạt động cuối cùng trong hệ thống.");
            }
        }

        // 5. Cập nhật và ghi Audit Log
        var success = await _roleRepo.AssignRoleAsync(targetUser.MaTaiKhoan, request.MaVaiTro);
        if (success)
        {
            var logMessage = $"Admin #{currentUserId} đổi vai trò tài khoản '{targetUser.TenDangNhap}' từ [{oldRoleCode}] sang [{newRoleCode}]";
            await _roleRepo.WriteAuditLogAsync("PhanQuyenVaiTro", logMessage, currentUserId);
        }

        return success;
    }

    /// <summary>
    /// Cập nhật ma trận quyền hạn cho một vai trò trong transaction.
    /// </summary>
    public async Task<bool> UpdateRolePermissionsAsync(long currentUserId, int roleId, UpdateRolePermissionsRequest request)
    {
        if (currentUserId <= 0)
        {
            throw new UnauthorizedAccessException("Không xác định được danh tính người thực hiện từ token đăng nhập.");
        }

        // 1. Kiểm tra vai trò
        var role = await _roleRepo.GetRoleByIdAsync(roleId);
        if (role == null)
        {
            throw new KeyNotFoundException($"Vai trò với mã #{roleId} không tồn tại.");
        }

        var perms = request.Permissions ?? [];

        // 2. Kiểm tra trùng lặp mã quyền
        var duplicateIds = perms.GroupBy(p => p.MaQuyen).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicateIds.Count > 0)
        {
            throw new InvalidOperationException($"Phát hiện mã quyền bị trùng lặp trong yêu cầu: {string.Join(", ", duplicateIds)}");
        }

        // 3. Kiểm tra tính tồn tại của tất cả mã quyền gửi lên
        var requestedIds = perms.Select(p => p.MaQuyen).Distinct().ToList();
        if (requestedIds.Count > 0)
        {
            var existingIds = await _roleRepo.GetExistingPermissionIdsAsync(requestedIds);
            var nonExisting = requestedIds.Except(existingIds).ToList();
            if (nonExisting.Count > 0)
            {
                throw new KeyNotFoundException($"Mã quyền không tồn tại trong hệ thống: {string.Join(", ", nonExisting)}");
            }
        }

        // 4. Kiểm tra phạm vi dữ liệu hợp lệ: chỉ chấp nhận ALL, OWN, PARTIAL
        foreach (var item in perms)
        {
            if (!RoleConstants.Scopes.IsValid(item.PhamVi))
            {
                throw new InvalidOperationException($"Phạm vi dữ liệu '{item.PhamVi}' cho quyền #{item.MaQuyen} không hợp lệ. Chỉ chấp nhận ALL, OWN, hoặc PARTIAL.");
            }
        }

        // 5. Bảo vệ vai trò Quản trị viên (ADMIN) không bị tước bỏ quyền tối thiểu
        if (RoleConstants.IsAdmin(role.MaCode))
        {
            var allPerms = await _roleRepo.GetAllPermissionsAsync();
            var requiredAdminPermIds = allPerms
                .Where(p => (p.Resource == "ACCOUNT" && p.Action == "VIEW") ||
                            (p.Resource == "PERMISSION" && p.Action == "UPDATE"))
                .Select(p => p.MaQuyen)
                .ToList();

            var currentPermIds = perms.Select(p => p.MaQuyen).ToHashSet();
            var missingCorePerms = requiredAdminPermIds.Where(id => !currentPermIds.Contains(id)).ToList();
            if (missingCorePerms.Count > 0)
            {
                throw new InvalidOperationException("Vai trò Quản trị viên (ADMIN) phải giữ lại các quyền tối thiểu để quản trị tài khoản và phân quyền.");
            }
        }

        // 6. Thực hiện cập nhật trong Transaction
        using var tx = await _roleRepo.BeginTransactionAsync();
        try
        {
            var updated = await _roleRepo.UpdateRolePermissionsAsync(roleId, perms);
            await tx.CommitAsync();

            var logMessage = $"Admin #{currentUserId} cập nhật {perms.Count} quyền cho vai trò [{role.MaCode}] (#{roleId})";
            await _roleRepo.WriteAuditLogAsync("CapNhatQuyenVaiTro", logMessage, currentUserId);

            return updated;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}
