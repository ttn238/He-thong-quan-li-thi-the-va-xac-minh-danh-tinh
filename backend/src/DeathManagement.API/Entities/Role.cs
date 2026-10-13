namespace DeathManagement.API.Entities;

/// <summary>
/// Đại diện cho Vai trò người dùng trong hệ thống (ADMIN, CONG_AN, CONG_AN_DUYET, NHA_XAC, NGUOI_DAN)
/// </summary>
public class Role
{
    public int MaVaiTro { get; set; }
    public string TenVaiTro { get; set; } = string.Empty;
    public string? MaCode { get; set; }
    public string? MoTa { get; set; }
    public bool TrangThai { get; set; } = true;
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;

    // Navigation property
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
