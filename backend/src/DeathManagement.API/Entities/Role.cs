using System.ComponentModel.DataAnnotations.Schema;
using DeathManagement.API.Common;

namespace DeathManagement.API.Entities;

/// <summary>
/// Đại diện cho Vai trò người dùng trong hệ thống (ADMIN, CONG_AN, CONG_AN_DUYET, NHA_XAC, NGUOI_DAN)
/// Nguồn dữ liệu vai trò chuẩn lưu trữ trong CSDL (bảng VaiTro)
/// </summary>
public class Role
{
    public int MaVaiTro { get; set; }
    public string MaCode { get; set; } = string.Empty;
    public string TenVaiTro { get; set; } = string.Empty;
    public string? MoTa { get; set; }
    public bool TrangThai { get; set; } = true;
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public string Code => !string.IsNullOrEmpty(MaCode) ? MaCode : RoleConstants.GetCode(MaVaiTro);

    // Navigation properties
    public virtual ICollection<User> Users { get; set; } = new List<User>();
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
