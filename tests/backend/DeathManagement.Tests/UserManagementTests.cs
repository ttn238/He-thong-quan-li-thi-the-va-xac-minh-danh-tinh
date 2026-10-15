using DeathManagement.API.DTOs.User;
using DeathManagement.API.Entities;
using DeathManagement.API.Repositories.Interfaces;
using DeathManagement.API.Services;
using DeathManagement.API.Services.Interfaces;

namespace DeathManagement.Tests;

public class MockUserRepository : IUserRepository
{
    private readonly List<User> _users = new();

    public MockUserRepository()
    {
        _users.Add(new User
        {
            MaTaiKhoan = 1,
            TenDangNhap = "admin",
            HoTen = "Quản trị viên Hệ thống",
            Email = "admin@demo.local",
            SoDienThoai = "0900000001",
            CCCD = "001090000001",
            MaVaiTro = 1,
            Role = new Role { MaVaiTro = 1, TenVaiTro = "Quản trị hệ thống", MaCode = "ADMIN" },
            TrangThai = "HoatDong"
        });

        _users.Add(new User
        {
            MaTaiKhoan = 2,
            TenDangNhap = "canbo01",
            HoTen = "Nguyễn Văn An",
            Email = "canbo01@demo.local",
            SoDienThoai = "0900000002",
            CCCD = "001090000002",
            MaVaiTro = 2,
            Role = new Role { MaVaiTro = 2, TenVaiTro = "Công an / cán bộ xác minh", MaCode = "CONG_AN" },
            TrangThai = "HoatDong"
        });
    }

    public Task<User?> GetByIdAsync(long id) =>
        Task.FromResult(_users.FirstOrDefault(u => u.MaTaiKhoan == id));

    public Task<User?> GetByUsernameAsync(string username) =>
        Task.FromResult(_users.FirstOrDefault(u => u.TenDangNhap == username));

    public Task<User?> GetByEmailAsync(string email) =>
        Task.FromResult(_users.FirstOrDefault(u => u.Email == email));

    public Task<PagedResult<User>> GetPagedListAsync(UserFilterRequest filter)
    {
        var q = _users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(filter.Keyword))
            q = q.Where(u => u.TenDangNhap.Contains(filter.Keyword) || u.HoTen.Contains(filter.Keyword));

        if (filter.MaVaiTro.HasValue)
            q = q.Where(u => u.MaVaiTro == filter.MaVaiTro.Value);

        var total = q.Count();
        var items = q.Skip((filter.PageIndex - 1) * filter.PageSize).Take(filter.PageSize).ToList();

        return Task.FromResult(new PagedResult<User>
        {
            Items = items,
            TotalCount = total,
            PageIndex = filter.PageIndex,
            PageSize = filter.PageSize
        });
    }

    public Task<User> CreateAsync(User user)
    {
        user.MaTaiKhoan = _users.Max(u => u.MaTaiKhoan) + 1;
        _users.Add(user);
        return Task.FromResult(user);
    }

    public Task<User> UpdateAsync(User user)
    {
        var idx = _users.FindIndex(u => u.MaTaiKhoan == user.MaTaiKhoan);
        if (idx >= 0) _users[idx] = user;
        return Task.FromResult(user);
    }

    public Task<bool> UpdateStatusAsync(long id, string status)
    {
        var u = _users.FirstOrDefault(x => x.MaTaiKhoan == id);
        if (u == null) return Task.FromResult(false);
        u.TrangThai = status;
        return Task.FromResult(true);
    }

    public Task<bool> UpdatePasswordAsync(long id, string newPasswordHash)
    {
        var u = _users.FirstOrDefault(x => x.MaTaiKhoan == id);
        if (u == null) return Task.FromResult(false);
        u.MatKhauHash = newPasswordHash;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(long id)
    {
        var u = _users.FirstOrDefault(x => x.MaTaiKhoan == id);
        if (u == null) return Task.FromResult(false);
        u.TrangThai = "Khoa";
        return Task.FromResult(true);
    }

    public Task<bool> ExistsByUsernameAsync(string username, long? excludeId = null) =>
        Task.FromResult(_users.Any(u => (excludeId == null || u.MaTaiKhoan != excludeId.Value) && u.TenDangNhap == username));

    public Task<bool> ExistsByEmailAsync(string email, long? excludeId = null) =>
        Task.FromResult(_users.Any(u => (excludeId == null || u.MaTaiKhoan != excludeId.Value) && u.Email == email));

    public Task<bool> ExistsByPhoneAsync(string phone, long? excludeId = null) =>
        Task.FromResult(_users.Any(u => (excludeId == null || u.MaTaiKhoan != excludeId.Value) && u.SoDienThoai == phone));

    public Task<bool> ExistsByCccdAsync(string cccd, long? excludeId = null) =>
        Task.FromResult(_users.Any(u => (excludeId == null || u.MaTaiKhoan != excludeId.Value) && u.CCCD == cccd));
}

public class UserManagementTests
{
    private readonly IUserService _userService;

    public UserManagementTests()
    {
        _userService = new UserService(new MockUserRepository());
    }

    [Fact]
    public async Task GetAllUsers_ReturnsPagedResult()
    {
        var result = await _userService.GetPagedListAsync(new UserFilterRequest { PageIndex = 1, PageSize = 10 });
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 2);
        Assert.Contains(result.Items, u => u.TenDangNhap == "admin");
    }

    [Fact]
    public async Task GetById_Existing_ReturnsUserDto()
    {
        var user = await _userService.GetByIdAsync(1);
        Assert.NotNull(user);
        Assert.Equal("admin", user.TenDangNhap);
        Assert.Equal("ADMIN", user.MaCodeVaiTro);
    }

    [Fact]
    public async Task GetById_NonExisting_ReturnsNull()
    {
        var user = await _userService.GetByIdAsync(99999);
        Assert.Null(user);
    }

    [Fact]
    public async Task CreateUser_DuplicateUsername_ThrowsInvalidOperationException()
    {
        var req = new CreateUserRequest
        {
            TenDangNhap = "admin",
            MatKhau = "Secret@123",
            HoTen = "Trùng Admin",
            MaVaiTro = 2
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _userService.CreateAsync(req));
    }

    [Fact]
    public async Task CreateUser_DuplicateEmail_ThrowsInvalidOperationException()
    {
        var req = new CreateUserRequest
        {
            TenDangNhap = "user_new",
            MatKhau = "Secret@123",
            HoTen = "Người Dùng Mới",
            Email = "admin@demo.local",
            MaVaiTro = 5
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _userService.CreateAsync(req));
    }

    [Fact]
    public async Task Admin_CannotSelfLock_ThrowsInvalidOperationException()
    {
        var req = new ChangeUserStatusRequest { TrangThai = "Khoa" };
        await Assert.ThrowsAsync<InvalidOperationException>(() => _userService.ChangeStatusAsync(1, 1, req));
    }

    [Fact]
    public async Task Admin_CannotSelfDelete_ThrowsInvalidOperationException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _userService.DeleteAsync(1, 1));
    }

    [Fact]
    public async Task ResetPassword_WithValidPassword_Succeeds()
    {
        var req = new ResetPasswordRequest { MatKhauMoi = "NewSecurePassword@123" };
        var ok = await _userService.ResetPasswordAsync(1, 2, req);
        Assert.True(ok);
    }
}
