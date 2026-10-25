using DeathManagement.API.DTOs.Auth;

namespace DeathManagement.API.Services.Interfaces;

public enum KetQuaDangNhap
{
    ThanhCong,
    SaiThongTin,
    TaiKhoanKhoa,
    TamKhoa
}

public interface IAuthService
{
    Task<(KetQuaDangNhap Ket, LoginResponse? TaiKhoan, string? ThongBao)> DangNhapAsync(
        string tenDangNhap,
        string matKhau);
}
