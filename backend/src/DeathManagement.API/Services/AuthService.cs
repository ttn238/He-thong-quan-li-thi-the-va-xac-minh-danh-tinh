using DeathManagement.API.Data;
using DeathManagement.API.DTOs.Auth;
using DeathManagement.API.Security;
using DeathManagement.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DeathManagement.API.Services;

public class AuthService(ApplicationDbContext db) : IAuthService
{
    private const int SoLanSaiToiDa = 5;     // sai 5 lần -> khóa
    private const int PhutKhoa = 15;         // khóa 15 phút
    private const string HoatDong = "HoatDong";
    private const string KhoaTam = "KhoaTam";

    private const string ThongBaoSai = "Sai tên đăng nhập hoặc mật khẩu.";
    private const string ThongBaoKhoa = "Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên để được mở khóa.";

    public async Task<(KetQuaDangNhap Ket, LoginResponse? TaiKhoan, string? ThongBao)> DangNhapAsync(
        string tenDangNhap, string matKhau)
    {
        var u = tenDangNhap.Trim();
        var ten = u.Length > 100 ? u[..100] : u;
        var now = DateTime.Now;   // cùng kiểu giờ với SYSDATETIME() của SQL Server

        // Có theo dõi (tracking) để cập nhật số lần sai / thời điểm khóa
        var tk = await db.Users.Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.TenDangNhap == u || x.Email == u);

        // 1. Không tồn tại: trả thông báo chung, không lộ tài khoản có tồn tại hay không
        if (tk is null)
        {
            await GhiNhatKyAsync(null, $"That bai (khong ton tai): {ten}");
            return (KetQuaDangNhap.SaiThongTin, null, ThongBaoSai);
        }

        // 2. Đang bị khóa tạm do nhập sai
        if (tk.TrangThai == KhoaTam && tk.KhoaDenThoiDiem is DateTime den)
        {
            if (den > now)
            {
                await GhiNhatKyAsync(tk.MaTaiKhoan, $"Bi tu choi (dang khoa tam): {ten}");
                return (KetQuaDangNhap.TamKhoa, null, ThongBaoKhoaTam(den, now));
            }
            // Hết hạn khóa -> tự mở lại (được lưu ở bước bên dưới)
            tk.TrangThai = HoatDong;
            tk.SoLanDangNhapSai = 0;
            tk.KhoaDenThoiDiem = null;
        }

        // 3. Sai mật khẩu
        if (!KiemTraMatKhau(matKhau, tk.MatKhauHash))
        {
            string? thongBao = ThongBaoSai;
            var ket = KetQuaDangNhap.SaiThongTin;

            if (tk.TrangThai == HoatDong)   // tài khoản đã bị Admin khóa thì không đếm
            {
                tk.SoLanDangNhapSai++;
                if (tk.SoLanDangNhapSai >= SoLanSaiToiDa)
                {
                    tk.TrangThai = KhoaTam;
                    tk.KhoaDenThoiDiem = now.AddMinutes(PhutKhoa);
                    tk.SoLanDangNhapSai = 0;
                    ket = KetQuaDangNhap.TamKhoa;
                    thongBao = ThongBaoKhoaTam(tk.KhoaDenThoiDiem.Value, now);
                }
                await db.SaveChangesAsync();
            }

            await GhiNhatKyAsync(tk.MaTaiKhoan, $"That bai (sai mat khau): {ten}");
            return (ket, null, thongBao);
        }

        // 4. Đúng mật khẩu nhưng tài khoản bị khóa hẳn (Khoa) hoặc khóa tạm không thời hạn
        if (tk.TrangThai != HoatDong)
        {
            await GhiNhatKyAsync(tk.MaTaiKhoan, $"Tai khoan bi khoa: {ten}");
            return (KetQuaDangNhap.TaiKhoanKhoa, null, ThongBaoKhoa);
        }

        // 5. Thành công
        tk.SoLanDangNhapSai = 0;
        tk.KhoaDenThoiDiem = null;
        tk.LanDangNhapCuoi = now;
        await db.SaveChangesAsync();

        await GhiNhatKyAsync(tk.MaTaiKhoan, $"Thanh cong: {ten}");
        return (KetQuaDangNhap.ThanhCong,
            new LoginResponse(tk.MaTaiKhoan, tk.TenDangNhap, tk.HoTen, tk.Role?.TenVaiTro ?? "", tk.DonVi),
            null);
    }

    // Hash sai định dạng thì coi như sai mật khẩu, không để form văng lỗi
    private static bool KiemTraMatKhau(string matKhau, string hash)
    {
        try { return PasswordHasher.Verify(matKhau, hash); }
        catch (FormatException) { return false; }
    }

    private static string ThongBaoKhoaTam(DateTime den, DateTime now)
    {
        var phut = Math.Max(1, (int)Math.Ceiling((den - now).TotalMinutes));
        return $"Tài khoản bị khóa tạm thời do nhập sai mật khẩu quá {SoLanSaiToiDa} lần. Vui lòng thử lại sau {phut} phút.";
    }

    // Dùng thủ tục sp_GhiNhatKy có sẵn trong 06_procedures.sql
    private Task GhiNhatKyAsync(long? maTaiKhoan, string doiTuong)
    {
        var hanhDong = "DangNhap";
        return db.Database.ExecuteSqlInterpolatedAsync($"EXEC sp_GhiNhatKy {hanhDong}, {doiTuong}, {maTaiKhoan}");
    }
}
