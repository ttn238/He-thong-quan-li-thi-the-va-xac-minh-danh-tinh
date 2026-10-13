namespace DeathManagement.API.Entities;

/// <summary>
/// Đại diện cho Tài khoản người dùng / Cán bộ trong hệ thống (TaiKhoanNguoiDung)
/// </summary>
public class User
{
    public long MaTaiKhoan { get; set; }
    public string TenDangNhap { get; set; } = string.Empty;
    public string MatKhauHash { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? SoDienThoai { get; set; }
    public string? CCCD { get; set; }
    public string? DonVi { get; set; }
    public string? ChucVu { get; set; }

    public int MaVaiTro { get; set; }
    public string TrangThai { get; set; } = "HoatDong"; // HoatDong, KhoaTam, Khoa

    public int SoLanDangNhapSai { get; set; } = 0;
    public DateTime? KhoaDenThoiDiem { get; set; }
    public DateTime? LanDangNhapCuoi { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
    public DateTime NgayCapNhat { get; set; } = DateTime.UtcNow;

    // Navigation property
    public virtual Role? Role { get; set; }
}
