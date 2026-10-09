namespace DeathManagement.API.Entities;

public class User
{
    public long MaTaiKhoan { get; set; }
    public string TenDangNhap { get; set; } = "";
    public string? Email { get; set; }
    public string? SoDienThoai { get; set; }
    public string MatKhauHash { get; set; } = "";
    public string HoTen { get; set; } = "";
    public string? DonVi { get; set; }
    public string TrangThai { get; set; } = "HoatDong";
    public int MaVaiTro { get; set; }
    public Role Role { get; set; } = null!;
}