using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace DeathManagement.API.Security;

public sealed class JwtOptions
{
    public string Key { get; set; } = string.Empty;           // >= 32 ký tự; để trống => khóa ngẫu nhiên mỗi lần chạy
    public string Issuer { get; set; } = "DeathManagement.API";
    public string Audience { get; set; } = "DeathManagement.Client";
    public int ExpireMinutes { get; set; } = 480;             // 8 giờ (1 ca làm việc)
}

/// <summary>
/// Tạo / xác thực JWT. Token mang danh tính (sub, name, username, unit), vai trò (role)
/// và danh sách quyền (perm = "RESOURCE:ACTION:SCOPE") của người dùng đã xác thực.
/// </summary>
public static class JwtTokenService
{
    public const string ClaimSub = "sub";
    public const string ClaimName = "name";
    public const string ClaimUsername = "username";
    public const string ClaimRole = "role";
    public const string ClaimUnit = "unit";
    public const string ClaimPerm = "perm";

    private static readonly object _sync = new();
    private static JwtOptions _options = new();
    private static SymmetricSecurityKey? _key;

    public static void Configure(JwtOptions? options = null)
    {
        var o = options ?? new JwtOptions();
        var keyBytes = Encoding.UTF8.GetBytes(o.Key ?? string.Empty);
        // Ứng dụng desktop: token chỉ dùng trong tiến trình => khóa ngẫu nhiên là đủ và không phải lưu bí mật trong mã nguồn
        if (keyBytes.Length < 32) keyBytes = RandomNumberGenerator.GetBytes(32);

        lock (_sync)
        {
            _options = o;
            _key = new SymmetricSecurityKey(keyBytes);
        }
    }

    private static void EnsureConfigured()
    {
        if (_key == null) Configure();
    }

    public static string CreateToken(long maTaiKhoan, string tenDangNhap, string hoTen, string roleCode, string? donVi)
    {
        EnsureConfigured();

        var claims = new List<Claim>
        {
            new(ClaimSub, maTaiKhoan.ToString(CultureInfo.InvariantCulture)),
            new(ClaimUsername, tenDangNhap ?? string.Empty),
            new(ClaimName, hoTen ?? string.Empty),
            new(ClaimRole, roleCode ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };
        if (!string.IsNullOrWhiteSpace(donVi)) claims.Add(new Claim(ClaimUnit, donVi));
        foreach (var p in AuthorizationPolicies.BuildPermissionClaims(roleCode))
            claims.Add(new Claim(ClaimPerm, p));

        var now = DateTime.UtcNow;
        var jwt = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(_options.ExpireMinutes),
            signingCredentials: new SigningCredentials(_key!, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    /// <summary>Trả về ClaimsPrincipal nếu token hợp lệ (đúng chữ ký, chưa hết hạn); ngược lại trả về null.</summary>
    public static ClaimsPrincipal? Validate(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        EnsureConfigured();

        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false }; // giữ nguyên tên claim "sub", "role"...
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _options.Issuer,
            ValidateAudience = true,
            ValidAudience = _options.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _key,
            ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = ClaimName,
            RoleClaimType = ClaimRole
        };

        try
        {
            return handler.ValidateToken(token, parameters, out _);
        }
        catch (Exception)
        {
            return null; // sai chữ ký / hết hạn / bị sửa => coi như chưa đăng nhập
        }
    }
}