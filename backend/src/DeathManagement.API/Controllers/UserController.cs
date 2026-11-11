using System.Security.Claims;
using DeathManagement.API.DTOs.User;
using DeathManagement.API.Services;
using DeathManagement.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeathManagement.API.Controllers;

[ApiController]
[Route("api/v1/users")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService? userService = null)
    {
        _userService = userService ?? new UserService();
    }

    /// <summary>
    /// Lấy danh sách tài khoản có phân trang, tìm kiếm, lọc (Chỉ Quản trị viên).
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "ADMIN,Quản trị hệ thống,QuanTriVien")]
    public async Task<ActionResult<PagedResult<UserDto>>> GetAll([FromQuery] UserFilterRequest filter)
    {
        var result = await _userService.GetPagedListAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Lấy thông tin cá nhân của người dùng đang đăng nhập.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetMyProfile()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new { message = "Không xác định được danh tính người dùng." });

        var user = await _userService.GetByIdAsync(userId.Value);
        if (user == null) return NotFound(new { message = "Không tìm thấy thông tin tài khoản." });

        return Ok(user);
    }

    /// <summary>
    /// Người dùng tự cập nhật thông tin cá nhân của mình (họ tên, email, SĐT).
    /// </summary>
    [HttpPut("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> UpdateMyProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new { message = "Không xác định được danh tính người dùng." });

        try
        {
            var updated = await _userService.UpdateProfileAsync(userId.Value, request);
            if (updated == null) return NotFound(new { message = "Không tìm thấy thông tin tài khoản." });
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy thông tin chi tiết một tài khoản theo ID (Chỉ Quản trị viên).
    /// </summary>
    [HttpGet("{id:long}")]
    [Authorize(Roles = "ADMIN,Quản trị hệ thống,QuanTriVien")]
    public async Task<ActionResult<UserDto>> GetById(long id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user == null) return NotFound(new { message = $"Không tìm thấy tài khoản với mã #{id}." });
        return Ok(user);
    }

    /// <summary>
    /// Tạo mới một tài khoản (Chỉ Quản trị viên).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "ADMIN,Quản trị hệ thống,QuanTriVien")]
    public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var created = await _userService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = created.MaTaiKhoan }, created);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Cập nhật thông tin tài khoản và phân quyền (Chỉ Quản trị viên).
    /// </summary>
    [HttpPut("{id:long}")]
    [Authorize(Roles = "ADMIN,Quản trị hệ thống,QuanTriVien")]
    public async Task<ActionResult<UserDto>> Update(long id, [FromBody] UpdateUserRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var updated = await _userService.UpdateAsync(id, request);
            if (updated == null) return NotFound(new { message = $"Không tìm thấy tài khoản với mã #{id}." });
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Khóa / Mở khóa trạng thái tài khoản (Chỉ Quản trị viên).
    /// </summary>
    [HttpPut("{id:long}/status")]
    [Authorize(Roles = "ADMIN,Quản trị hệ thống,QuanTriVien")]
    public async Task<IActionResult> ChangeStatus(long id, [FromBody] ChangeUserStatusRequest request)
    {
        var currentUserId = GetCurrentUserId() ?? 0;
        try
        {
            var success = await _userService.ChangeStatusAsync(currentUserId, id, request);
            if (!success) return NotFound(new { message = $"Không tìm thấy tài khoản với mã #{id}." });
            return Ok(new { message = $"Đã chuyển trạng thái tài khoản sang '{request.TrangThai}' thành công." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Đặt lại mật khẩu tài khoản (Chỉ Quản trị viên).
    /// </summary>
    [HttpPut("{id:long}/reset-password")]
    [Authorize(Roles = "ADMIN,Quản trị hệ thống,QuanTriVien")]
    public async Task<IActionResult> ResetPassword(long id, [FromBody] ResetPasswordRequest request)
    {
        var currentUserId = GetCurrentUserId() ?? 0;
        try
        {
            var success = await _userService.ResetPasswordAsync(currentUserId, id, request);
            if (!success) return NotFound(new { message = $"Không tìm thấy tài khoản với mã #{id}." });
            return Ok(new { message = "Đã đặt lại mật khẩu mới cho tài khoản thành công." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Xóa tài khoản (Chỉ Quản trị viên).
    /// </summary>
    [HttpDelete("{id:long}")]
    [Authorize(Roles = "ADMIN,Quản trị hệ thống,QuanTriVien")]
    public async Task<IActionResult> Delete(long id)
    {
        var currentUserId = GetCurrentUserId() ?? 0;
        try
        {
            var success = await _userService.DeleteAsync(currentUserId, id);
            if (!success) return NotFound(new { message = $"Không tìm thấy tài khoản với mã #{id}." });
            return Ok(new { message = $"Đã xóa tài khoản #{id} thành công." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
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
