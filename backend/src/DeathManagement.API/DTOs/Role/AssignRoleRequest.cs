using System.ComponentModel.DataAnnotations;

namespace DeathManagement.API.DTOs.Role;

public class AssignRoleRequest
{
    [Required(ErrorMessage = "Mã tài khoản không được để trống.")]
    public long MaTaiKhoan { get; set; }

    [Required(ErrorMessage = "Mã vai trò không được để trống.")]
    [Range(1, 100, ErrorMessage = "Mã vai trò không hợp lệ.")]
    public int MaVaiTro { get; set; }

    public string? LyDo { get; set; }
}
