using System.ComponentModel.DataAnnotations;

namespace DeathManagement.API.DTOs.User;

/// <summary>
/// Yêu cầu đặt lại mật khẩu cho tài khoản bởi Quản trị viên
/// </summary>
public class ResetPasswordRequest
{
    [Required(ErrorMessage = "Mật khẩu mới không được để trống")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu mới phải từ 6 ký tự trở lên")]
    public string MatKhauMoi { get; set; } = string.Empty;
}
