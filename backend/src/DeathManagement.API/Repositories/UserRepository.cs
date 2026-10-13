using System.Collections.Concurrent;
using DeathManagement.API.DTOs.User;
using DeathManagement.API.Entities;
using DeathManagement.API.Repositories.Interfaces;

namespace DeathManagement.API.Repositories;

/// <summary>
/// Cài đặt kho lưu trữ dữ liệu tài khoản người dùng
/// Quản lý dữ liệu đồng bộ với cấu trúc bảng TaiKhoanNguoiDung và Seed Data hệ thống
/// </summary>
public class UserRepository : IUserRepository
{
    private static readonly ConcurrentDictionary<long, User> _users = new();
    private static readonly ConcurrentDictionary<int, Role> _roles = new();
    private static long _lastId = 0;
    private static readonly object _lock = new();

    static UserRepository()
    {
        InitializeSeedData();
    }

    private static void InitializeSeedData()
    {
        var rolesList = new List<Role>
        {
            new() { MaVaiTro = 1, TenVaiTro = "Quản trị hệ thống", MaCode = "ADMIN" },
            new() { MaVaiTro = 2, TenVaiTro = "Công an / cán bộ xác minh", MaCode = "CONG_AN" },
            new() { MaVaiTro = 3, TenVaiTro = "Công an có thẩm quyền duyệt", MaCode = "CONG_AN_DUYET" },
            new() { MaVaiTro = 4, TenVaiTro = "Nhân viên nhà xác", MaCode = "NHA_XAC" },
            new() { MaVaiTro = 5, TenVaiTro = "Người dân / người thân", MaCode = "NGUOI_DAN" }
        };

        foreach (var r in rolesList)
        {
            _roles[r.MaVaiTro] = r;
        }

        const string defaultHash = "100000.kh6sXiu8EcrIcTL5NBOzvg==.SwFY+yE3YNgbiVR3IgzeoadrUXTfa1/PY4S/3a2Rmaw=";

        var seedUsers = new List<User>
        {
            new()
            {
                MaTaiKhoan = 1,
                TenDangNhap = "admin",
                HoTen = "Quản trị viên Hệ thống",
                Email = "admin@demo.local",
                SoDienThoai = "0900000001",
                CCCD = "001090000001",
                DonVi = "Phòng Công nghệ thông tin",
                ChucVu = "Quản trị viên",
                MaVaiTro = 1,
                Role = _roles[1],
                MatKhauHash = defaultHash,
                TrangThai = "HoatDong",
                NgayTao = DateTime.UtcNow.AddMonths(-1),
                NgayCapNhat = DateTime.UtcNow
            },
            new()
            {
                MaTaiKhoan = 2,
                TenDangNhap = "canbo01",
                HoTen = "Nguyễn Văn An",
                Email = "canbo01@demo.local",
                SoDienThoai = "0900000002",
                CCCD = "001090000002",
                DonVi = "Công an Quận 1",
                ChucVu = "Cán bộ điều tra",
                MaVaiTro = 2,
                Role = _roles[2],
                MatKhauHash = defaultHash,
                TrangThai = "HoatDong",
                NgayTao = DateTime.UtcNow.AddDays(-20),
                NgayCapNhat = DateTime.UtcNow
            },
            new()
            {
                MaTaiKhoan = 3,
                TenDangNhap = "canbo02",
                HoTen = "Trần Thị Bích",
                Email = "canbo02@demo.local",
                SoDienThoai = "0900000003",
                CCCD = "001090000003",
                DonVi = "Công an Quận 1",
                ChucVu = "Chỉ huy đội xác minh",
                MaVaiTro = 3,
                Role = _roles[3],
                MatKhauHash = defaultHash,
                TrangThai = "HoatDong",
                NgayTao = DateTime.UtcNow.AddDays(-15),
                NgayCapNhat = DateTime.UtcNow
            },
            new()
            {
                MaTaiKhoan = 4,
                TenDangNhap = "nhaxac01",
                HoTen = "Lê Văn Cường",
                Email = "nhaxac01@demo.local",
                SoDienThoai = "0900000004",
                CCCD = "001090000004",
                DonVi = "Nhà xác Trung tâm",
                ChucVu = "Cán bộ quản lý kho",
                MaVaiTro = 4,
                Role = _roles[4],
                MatKhauHash = defaultHash,
                TrangThai = "HoatDong",
                NgayTao = DateTime.UtcNow.AddDays(-10),
                NgayCapNhat = DateTime.UtcNow
            },
            new()
            {
                MaTaiKhoan = 5,
                TenDangNhap = "dan01",
                HoTen = "Phạm Thị Duyên",
                Email = "dan01@demo.local",
                SoDienThoai = "0900000005",
                CCCD = "001090000005",
                MaVaiTro = 5,
                Role = _roles[5],
                MatKhauHash = defaultHash,
                TrangThai = "HoatDong",
                NgayTao = DateTime.UtcNow.AddDays(-5),
                NgayCapNhat = DateTime.UtcNow
            },
            new()
            {
                MaTaiKhoan = 6,
                TenDangNhap = "dan02",
                HoTen = "Hoàng Văn Em",
                Email = "dan02@demo.local",
                SoDienThoai = "0900000006",
                CCCD = "001090000006",
                MaVaiTro = 5,
                Role = _roles[5],
                MatKhauHash = defaultHash,
                TrangThai = "HoatDong",
                NgayTao = DateTime.UtcNow.AddDays(-2),
                NgayCapNhat = DateTime.UtcNow
            }
        };

        foreach (var u in seedUsers)
        {
            _users[u.MaTaiKhoan] = u;
            if (u.MaTaiKhoan > _lastId) _lastId = u.MaTaiKhoan;
        }
    }

    public Task<User?> GetByIdAsync(long id)
    {
        _users.TryGetValue(id, out var user);
        return Task.FromResult(user);
    }

    public Task<User?> GetByUsernameAsync(string username)
    {
        var user = _users.Values.FirstOrDefault(u => 
            string.Equals(u.TenDangNhap, username, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(user);
    }

    public Task<User?> GetByEmailAsync(string email)
    {
        var user = _users.Values.FirstOrDefault(u => 
            string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(user);
    }

    public Task<PagedResult<User>> GetPagedListAsync(UserFilterRequest filter)
    {
        var query = _users.Values.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var kw = filter.Keyword.Trim();
            query = query.Where(u =>
                u.TenDangNhap.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                u.HoTen.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                (u.Email != null && u.Email.Contains(kw, StringComparison.OrdinalIgnoreCase)) ||
                (u.SoDienThoai != null && u.SoDienThoai.Contains(kw, StringComparison.OrdinalIgnoreCase)) ||
                (u.CCCD != null && u.CCCD.Contains(kw, StringComparison.OrdinalIgnoreCase)));
        }

        if (filter.MaVaiTro.HasValue && filter.MaVaiTro.Value > 0)
        {
            query = query.Where(u => u.MaVaiTro == filter.MaVaiTro.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.TrangThai))
        {
            query = query.Where(u => string.Equals(u.TrangThai, filter.TrangThai, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filter.DonVi))
        {
            query = query.Where(u => u.DonVi != null && u.DonVi.Contains(filter.DonVi, StringComparison.OrdinalIgnoreCase));
        }

        var totalCount = query.Count();
        var pageIndex = filter.PageIndex > 0 ? filter.PageIndex : 1;
        var pageSize = filter.PageSize > 0 ? filter.PageSize : 10;

        var items = query
            .OrderByDescending(u => u.MaTaiKhoan)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var result = new PagedResult<User>
        {
            Items = items,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize
        };

        return Task.FromResult(result);
    }

    public Task<User> CreateAsync(User user)
    {
        lock (_lock)
        {
            user.MaTaiKhoan = ++_lastId;
            user.NgayTao = DateTime.UtcNow;
            user.NgayCapNhat = DateTime.UtcNow;

            if (_roles.TryGetValue(user.MaVaiTro, out var role))
            {
                user.Role = role;
            }

            _users[user.MaTaiKhoan] = user;
            return Task.FromResult(user);
        }
    }

    public Task<User> UpdateAsync(User user)
    {
        user.NgayCapNhat = DateTime.UtcNow;
        if (_roles.TryGetValue(user.MaVaiTro, out var role))
        {
            user.Role = role;
        }

        _users[user.MaTaiKhoan] = user;
        return Task.FromResult(user);
    }

    public Task<bool> UpdateStatusAsync(long id, string status)
    {
        if (_users.TryGetValue(id, out var user))
        {
            user.TrangThai = status;
            user.NgayCapNhat = DateTime.UtcNow;
            if (status == "HoatDong")
            {
                user.SoLanDangNhapSai = 0;
                user.KhoaDenThoiDiem = null;
            }
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    public Task<bool> UpdatePasswordAsync(long id, string newPasswordHash)
    {
        if (_users.TryGetValue(id, out var user))
        {
            user.MatKhauHash = newPasswordHash;
            user.NgayCapNhat = DateTime.UtcNow;
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    public Task<bool> DeleteAsync(long id)
    {
        // Thực hiện xóa mềm bằng cách đổi trạng thái sang "Khoa"
        if (_users.TryGetValue(id, out var user))
        {
            user.TrangThai = "Khoa";
            user.NgayCapNhat = DateTime.UtcNow;
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    public Task<bool> ExistsByUsernameAsync(string username, long? excludeId = null)
    {
        var exists = _users.Values.Any(u =>
            (excludeId == null || u.MaTaiKhoan != excludeId.Value) &&
            string.Equals(u.TenDangNhap, username, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(exists);
    }

    public Task<bool> ExistsByEmailAsync(string email, long? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(email)) return Task.FromResult(false);
        var exists = _users.Values.Any(u =>
            (excludeId == null || u.MaTaiKhoan != excludeId.Value) &&
            string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(exists);
    }

    public Task<bool> ExistsByPhoneAsync(string phone, long? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(phone)) return Task.FromResult(false);
        var exists = _users.Values.Any(u =>
            (excludeId == null || u.MaTaiKhoan != excludeId.Value) &&
            string.Equals(u.SoDienThoai, phone, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(exists);
    }

    public Task<bool> ExistsByCccdAsync(string cccd, long? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(cccd)) return Task.FromResult(false);
        var exists = _users.Values.Any(u =>
            (excludeId == null || u.MaTaiKhoan != excludeId.Value) &&
            string.Equals(u.CCCD, cccd, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(exists);
    }
}
