namespace DeathManagement.API.DTOs.Auth;

public record LoginResponse(long MaTaiKhoan, string TenDangNhap, string HoTen, string TenVaiTro, string? DonVi);