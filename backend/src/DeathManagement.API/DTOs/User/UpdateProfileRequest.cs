using System.ComponentModel.DataAnnotations;

namespace DeathManagement.API.DTOs.User;

/// <summary>
/// Yêu cầu cập nhật hồ sơ cá nhân của chính người dùng (PUT /users/me)
/// </summary>
public class UpdateProfileRequest
{
    [Required(ErrorMessage = "Họ và tên không được để trống")]
    [StringLength(150, ErrorMessage = "Họ và tên không vượt quá 150 ký tự")]
    public string HoTen { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ")]
    public string? Email { get; set; }

    [RegularExpression(@"^[0-9+]{9,15}$", ErrorMessage = "Số điện thoại không hợp lệ (9-15 chữ số)")]
    public string? SoDienThoai { get; set; }
}
