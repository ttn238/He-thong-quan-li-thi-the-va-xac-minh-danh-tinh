using System.ComponentModel.DataAnnotations;

namespace DeathManagement.API.DTOs.User;

/// <summary>
/// Yêu cầu tạo mới tài khoản bởi Quản trị viên
/// </summary>
public class CreateUserRequest
{
    [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập phải từ 3 đến 50 ký tự")]
    [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Tên đăng nhập chỉ chứa chữ cái, số và dấu gạch dưới")]
    public string TenDangNhap { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu không được để trống")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
    public string MatKhau { get; set; } = string.Empty;

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
