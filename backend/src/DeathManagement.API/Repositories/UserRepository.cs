using DeathManagement.API.Data;
using DeathManagement.API.DTOs.User;
using DeathManagement.API.Entities;
using DeathManagement.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DeathManagement.API.Repositories;

/// <summary>
/// Kho lưu trữ dữ liệu tài khoản người dùng tương tác trực tiếp CSDL qua Entity Framework Core
/// Tuân thủ thiết kế bảng TaiKhoanNguoiDung trong CSDL QL_XACMINHTHITHE
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _db;

    public UserRepository(ApplicationDbContext? db = null)
    {
        _db = db ?? Program.NewDb();
    }

    public async Task<User?> GetByIdAsync(long id)
    {
        return await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.MaTaiKhoan == id);
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username)) return null;
        var clean = username.Trim();

        return await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.TenDangNhap == clean);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        var clean = email.Trim();

        return await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == clean);
    }

    public async Task<PagedResult<User>> GetPagedListAsync(UserFilterRequest filter)
    {
        var q = _db.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .AsQueryable();

        // 1. Tìm kiếm từ khóa
        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var kw = filter.Keyword.Trim();
            q = q.Where(u => u.TenDangNhap.Contains(kw) ||
                             u.HoTen.Contains(kw) ||
                             (u.Email != null && u.Email.Contains(kw)) ||
                             (u.CCCD != null && u.CCCD.Contains(kw)) ||
                             (u.SoDienThoai != null && u.SoDienThoai.Contains(kw)));
        }

        // 2. Lọc vai trò
        if (filter.MaVaiTro.HasValue && filter.MaVaiTro.Value > 0)
        {
            q = q.Where(u => u.MaVaiTro == filter.MaVaiTro.Value);
        }

        // 3. Lọc trạng thái
        if (!string.IsNullOrWhiteSpace(filter.TrangThai) && !filter.TrangThai.StartsWith("Tất cả", StringComparison.OrdinalIgnoreCase))
        {
            q = q.Where(u => u.TrangThai == filter.TrangThai);
        }

        // 4. Lọc đơn vị công tác
        if (!string.IsNullOrWhiteSpace(filter.DonVi))
        {
            var dv = filter.DonVi.Trim();
            q = q.Where(u => u.DonVi != null && u.DonVi.Contains(dv));
        }

        var totalCount = await q.CountAsync();
        var pageIndex = filter.PageIndex > 0 ? filter.PageIndex : 1;
        var pageSize = filter.PageSize > 0 ? filter.PageSize : 10;

        var items = await q
            .OrderByDescending(u => u.MaTaiKhoan)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<User>
        {
            Items = items,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }

    public async Task<User> CreateAsync(User user)
    {
        user.NgayTao = DateTime.UtcNow;
        user.NgayCapNhat = DateTime.UtcNow;

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        if (user.Role == null)
        {
            await _db.Entry(user).Reference(u => u.Role).LoadAsync();
        }

        return user;
    }

    public async Task<User> UpdateAsync(User user)
    {
        user.NgayCapNhat = DateTime.UtcNow;

        _db.Users.Update(user);
        await _db.SaveChangesAsync();

        if (user.Role == null)
        {
            await _db.Entry(user).Reference(u => u.Role).LoadAsync();
        }

        return user;
    }

    public async Task<bool> UpdateStatusAsync(long id, string status)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.MaTaiKhoan == id);
        if (user == null) return false;

        user.TrangThai = status;
        user.NgayCapNhat = DateTime.UtcNow;

        if (status == "HoatDong")
        {
            user.SoLanDangNhapSai = 0;
            user.KhoaDenThoiDiem = null;
        }

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdatePasswordAsync(long id, string newPasswordHash)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.MaTaiKhoan == id);
        if (user == null) return false;

        user.MatKhauHash = newPasswordHash;
        user.NgayCapNhat = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.MaTaiKhoan == id);
        if (user == null) return false;

        // Xóa mềm: Chuyển trạng thái sang Khoa để bảo toàn toàn vẹn tham chiếu CSDL
        user.TrangThai = "Khoa";
        user.NgayCapNhat = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsByUsernameAsync(string username, long? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(username)) return false;
        var clean = username.Trim();

        return await _db.Users.AnyAsync(u =>
            (excludeId == null || u.MaTaiKhoan != excludeId.Value) &&
            u.TenDangNhap == clean);
    }

    public async Task<bool> ExistsByEmailAsync(string email, long? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        var clean = email.Trim();

        return await _db.Users.AnyAsync(u =>
            (excludeId == null || u.MaTaiKhoan != excludeId.Value) &&
            u.Email == clean);
    }

    public async Task<bool> ExistsByPhoneAsync(string phone, long? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(phone)) return false;
        var clean = phone.Trim();

        return await _db.Users.AnyAsync(u =>
            (excludeId == null || u.MaTaiKhoan != excludeId.Value) &&
            u.SoDienThoai == clean);
    }

    public async Task<bool> ExistsByCccdAsync(string cccd, long? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(cccd)) return false;
        var clean = cccd.Trim();

        return await _db.Users.AnyAsync(u =>
            (excludeId == null || u.MaTaiKhoan != excludeId.Value) &&
            u.CCCD == clean);
    }
}
