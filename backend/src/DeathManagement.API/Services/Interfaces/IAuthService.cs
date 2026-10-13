using DeathManagement.API.DTOs.Auth;

namespace DeathManagement.API.Services.Interfaces;

public enum KetQuaDangNhap { ThanhCong, SaiThongTin, TaiKhoanKhoa }

public interface IAuthService
{
    Task<(KetQuaDangNhap Ket, LoginResponse? TaiKhoan)> DangNhapAsync(string tenDangNhap, string matKhau);
}