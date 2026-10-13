using System.ComponentModel.DataAnnotations;

namespace DeathManagement.API.DTOs.User;

/// <summary>
/// Yêu cầu thay đổi trạng thái tài khoản (Khóa / Mở khóa) bởi Quản trị viên
/// </summary>
public class ChangeUserStatusRequest
{
    [Required(ErrorMessage = "Trạng thái mới không được để trống")]
    [RegularExpression(@"^(HoatDong|Khoa)$", ErrorMessage = "Trạng thái chỉ có thể là 'HoatDong' hoặc 'Khoa'")]
    public string TrangThai { get; set; } = "HoatDong";

    [StringLength(255, ErrorMessage = "Lý do không vượt quá 255 ký tự")]
    public string? LyDo { get; set; }
}
