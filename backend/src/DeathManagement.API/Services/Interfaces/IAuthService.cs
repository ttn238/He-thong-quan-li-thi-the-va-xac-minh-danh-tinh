using DeathManagement.API.DTOs.Auth;

namespace DeathManagement.API.Services.Interfaces;

public enum KetQuaDangNhap { ThanhCong, SaiThongTin, TaiKhoanKhoa, TamKhoa }

public interface IAuthService
{
    // ThongBao: câu hiển thị cho người dùng khi đăng nhập không thành công (có thể null)
    Task<(KetQuaDangNhap Ket, LoginResponse? TaiKhoan, string? ThongBao)> DangNhapAsync(string tenDangNhap, string matKhau);
}
