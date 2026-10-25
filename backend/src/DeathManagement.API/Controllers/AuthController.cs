using DeathManagement.API.DTOs.Auth;
using DeathManagement.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DeathManagement.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(IAuthService auth) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.TenDangNhap) || string.IsNullOrEmpty(req.MatKhau))
            return BadRequest(new { message = "Vui lòng nhập tên đăng nhập và mật khẩu." });

        var (ket, tk) = await auth.DangNhapAsync(req.TenDangNhap, req.MatKhau);
        return ket switch
        {
            KetQuaDangNhap.ThanhCong => Ok(tk),
            KetQuaDangNhap.TaiKhoanKhoa => StatusCode(StatusCodes.Status423Locked,
                new { message = "Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên để được mở khóa." }),
            _ => Unauthorized(new { message = "Sai tên đăng nhập hoặc mật khẩu." })
        };
    }
}