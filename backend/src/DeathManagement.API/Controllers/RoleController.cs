using System.Security.Claims;
using DeathManagement.API.Common;
using DeathManagement.API.DTOs.Role;
using DeathManagement.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DeathManagement.API.Controllers;

[ApiController]
[Route("api/v1/roles")]
[Authorize(Roles = RoleConstants.Admin)]
public class RoleController : ControllerBase
{
    private readonly IRoleService _roleService;

    public RoleController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    /// <summary>
    /// Lấy danh sách 5 vai trò hệ thống kèm số lượng tài khoản trực thuộc.
    /// GET /api/v1/roles
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<RoleDto>>> GetRoles()
    {
        var roles = await _roleService.GetRolesWithStatsAsync();
        return Ok(roles);
    }

    /// <summary>
    /// Lấy thông tin chi tiết một vai trò kèm danh sách quyền hạn (RBAC matrix).
    /// GET /api/v1/roles/{id}
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<RoleDto>> GetRoleDetails(int id)
    {
        var role = await _roleService.GetRoleDetailsAsync(id);
        if (role == null)
            return NotFound(new { message = $"Không tìm thấy vai trò với mã #{id}." });

        return Ok(role);
    }

    /// <summary>
    /// Lấy toàn bộ danh mục quyền hạn hệ thống phân theo nhóm Resource.
    /// GET /api/v1/roles/permissions
    /// </summary>
    [HttpGet("permissions")]
    public async Task<ActionResult<List<ResourceGroupedPermissionsDto>>> GetPermissions()
    {
        var groups = await _roleService.GetAllPermissionsGroupedAsync();
        return Ok(groups);
    }

    /// <summary>
    /// Gán hoặc thay đổi vai trò cho tài khoản người dùng (Chỉ Quản trị viên).
    /// PUT /api/v1/roles/assign
    /// </summary>
    [HttpPut("assign")]
    public async Task<IActionResult> AssignRole([FromBody] AssignRoleRequest request)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null || currentUserId <= 0)
        {
            return Unauthorized(new { message = "Yêu cầu đăng nhập hợp lệ với thông tin định danh người dùng." });
        }

        try
        {
            var success = await _roleService.AssignRoleAsync(currentUserId.Value, request);
            if (!success)
                return NotFound(new { message = $"Không tìm thấy tài khoản với mã #{request.MaTaiKhoan}." });

            return Ok(new
            {
                message = $"Đã gán vai trò #{request.MaVaiTro} cho tài khoản #{request.MaTaiKhoan} thành công."
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            // Vi phạm quy tắc an toàn (hạ quyền admin cuối cùng, tự đổi quyền chính mình) -> 409 Conflict
            return StatusCode(StatusCodes.Status409Conflict, new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Cập nhật ma trận phân quyền cho một vai trò.
    /// PUT /api/v1/roles/{id}/permissions
    /// </summary>
    [HttpPut("{id:int}/permissions")]
    public async Task<IActionResult> UpdatePermissions(int id, [FromBody] UpdateRolePermissionsRequest request)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null || currentUserId <= 0)
        {
            return Unauthorized(new { message = "Yêu cầu đăng nhập hợp lệ với thông tin định danh người dùng." });
        }

        try
        {
            var success = await _roleService.UpdateRolePermissionsAsync(currentUserId.Value, id, request);
            if (!success)
                return NotFound(new { message = $"Không tìm thấy vai trò với mã #{id}." });

            return Ok(new { message = $"Đã cập nhật danh sách quyền cho vai trò #{id} thành công." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            // Dữ liệu trùng lặp, phạm vi sai, hoặc tước quyền admin -> 400 hoặc 409
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    private long? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                 ?? User.FindFirst("sub")?.Value
                 ?? User.FindFirst("id")?.Value
                 ?? User.FindFirst("MaTaiKhoan")?.Value;

        return long.TryParse(claim, out var id) ? id : null;
    }
}
