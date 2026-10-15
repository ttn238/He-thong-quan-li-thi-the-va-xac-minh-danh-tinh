using System.ComponentModel.DataAnnotations;

namespace DeathManagement.API.DTOs.User;

/// <summary>
/// Yêu cầu cập nhật thông tin tài khoản bởi Quản trị viên
/// </summary>
public class UpdateUserRequest
{
    [Required(ErrorMessage = "Họ và tên không được để trống")]
    [StringLength(150, ErrorMessage = "Họ và tên không vượt quá 150 ký tự")]
    public string HoTen { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ")]
    public string? Email { get; set; }

    [RegularExpression(@"^[0-9+]{9,15}$", ErrorMessage = "Số điện thoại không hợp lệ (9-15 chữ số)")]
    public string? SoDienThoai { get; set; }

    [RegularExpression(@"^[0-9]{9,12}$", ErrorMessage = "Số CCCD/Định danh phải từ 9 đến 12 số")]
    public string? CCCD { get; set; }

    public string? DonVi { get; set; }
    public string? ChucVu { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn vai trò cho tài khoản")]
    public int MaVaiTro { get; set; }
}
