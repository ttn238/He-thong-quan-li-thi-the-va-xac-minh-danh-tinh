using DeathManagement.API.Data;
using DeathManagement.API.DTOs.Auth;
using DeathManagement.API.Security;
using DeathManagement.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DeathManagement.API.Services;

public class AuthService(ApplicationDbContext db) : IAuthService
{
    public async Task<(KetQuaDangNhap Ket, LoginResponse? TaiKhoan)> DangNhapAsync(string tenDangNhap, string matKhau)
    {
        var u = tenDangNhap.Trim();
        var ten = u.Length > 100 ? u[..100] : u;

        var tk = await db.Users.AsNoTracking().Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.TenDangNhap == u || x.Email == u);

        if (tk is null || !PasswordHasher.Verify(matKhau, tk.MatKhauHash))
        {
            await GhiNhatKyAsync(null, $"That bai: {ten}");
            return (KetQuaDangNhap.SaiThongTin, null);
        }

        if (tk.TrangThai != "HoatDong")   // Khoa hoặc KhoaTam
        {
            await GhiNhatKyAsync(tk.MaTaiKhoan, $"Tai khoan bi khoa: {ten}");
            return (KetQuaDangNhap.TaiKhoanKhoa, null);
        }

        await GhiNhatKyAsync(tk.MaTaiKhoan, $"Thanh cong: {ten}");
        return (KetQuaDangNhap.ThanhCong,
            new LoginResponse(tk.MaTaiKhoan, tk.TenDangNhap, tk.HoTen, tk.Role.TenVaiTro, tk.DonVi));
    }

    // Dùng thủ tục sp_GhiNhatKy có sẵn trong 06_procedures.sql
    private Task GhiNhatKyAsync(long? maTaiKhoan, string doiTuong)
    {
        var hanhDong = "DangNhap";
        return db.Database.ExecuteSqlInterpolatedAsync($"EXEC sp_GhiNhatKy {hanhDong}, {doiTuong}, {maTaiKhoan}");
    }
}