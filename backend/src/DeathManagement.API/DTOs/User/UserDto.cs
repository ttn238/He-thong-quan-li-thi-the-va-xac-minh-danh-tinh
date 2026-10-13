namespace DeathManagement.API.DTOs.User;

/// <summary>
/// DTO trả về thông tin người dùng an toàn (không bao gồm mật khẩu)
/// </summary>
public class UserDto
{
    public long MaTaiKhoan { get; set; }
    public string TenDangNhap { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? SoDienThoai { get; set; }
    public string? CCCD { get; set; }
    public string? DonVi { get; set; }
    public string? ChucVu { get; set; }
    public int MaVaiTro { get; set; }
    public string TenVaiTro { get; set; } = string.Empty;
    public string TrangThai { get; set; } = "HoatDong";
    public int SoLanDangNhapSai { get; set; }
    public DateTime? KhoaDenThoiDiem { get; set; }
    public DateTime? LanDangNhapCuoi { get; set; }
    public DateTime NgayTao { get; set; }
    public DateTime NgayCapNhat { get; set; }
}
