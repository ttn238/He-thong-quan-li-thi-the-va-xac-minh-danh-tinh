using System.Security.Claims;

namespace DeathManagement.API.Security;

/// <summary>401: chưa đăng nhập, token sai/bị sửa hoặc đã hết hạn.</summary>
public sealed class UnauthenticatedException : Exception
{
    public int StatusCode => 401;

    public UnauthenticatedException(string? message = null)
        : base(message ?? "Bạn chưa đăng nhập hoặc phiên làm việc đã hết hạn.") { }
}

/// <summary>403: đã đăng nhập nhưng không đủ quyền.</summary>
public sealed class ForbiddenException : Exception
{
    public int StatusCode => 403;
    public string? Resource { get; }
    public string? PermissionAction { get; }

    public ForbiddenException(string message, string? resource = null, string? action = null)
        : base(message)
    {
        Resource = resource;
        PermissionAction = action;
    }
}

/// <summary>
/// Cổng kiểm tra quyền DUY NHẤT của backend. Mọi service / EF interceptor đều gọi vào đây.
/// Danh tính + quyền lấy từ JWT đã được XÁC THỰC CHỮ KÝ (không tin cờ bool do UI truyền vào).
/// </summary>
public static class AccessControl
{
    /// <summary>
    /// Nguồn principal. Mặc định: JWT trong Session (WinForms).
    /// Bản ASP.NET Core có thể gán: AccessControl.PrincipalProvider = () => httpContext.User;
    /// </summary>
    public static Func<ClaimsPrincipal?> PrincipalProvider { get; set; } = FromSession;

    /// <summary>Phát sinh mỗi khi một yêu cầu bị từ chối (userId, resource, action, lý do) - dùng để ghi Audit Log.</summary>
    public static event Action<long?, string, string, string>? Denied;

    private static ClaimsPrincipal? FromSession() => JwtTokenService.Validate(Session.TaiKhoan?.Token);

    // ---------- Thông tin người dùng hiện tại ----------
    public static ClaimsPrincipal? Principal => PrincipalProvider();

    public static bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public static long? UserId => GetUserId(Principal);

    public static string? RoleCode => Principal?.FindFirst(JwtTokenService.ClaimRole)?.Value;

    private static long? GetUserId(ClaimsPrincipal? p)
        => long.TryParse(p?.FindFirst(JwtTokenService.ClaimSub)?.Value, out var id) ? id : null;

    // ---------- Truy vấn quyền ----------
    private static string? ScopeOf(ClaimsPrincipal? p, string resource, string action)
    {
        if (p == null) return null;
        var prefix = $"{resource}:{action}:";
        foreach (var c in p.FindAll(JwtTokenService.ClaimPerm))
            if (c.Value.StartsWith(prefix, StringComparison.Ordinal))
                return c.Value.Substring(prefix.Length);
        return null;
    }

    /// <summary>Phạm vi quyền hiện tại (ALL / OWN / PARTIAL) hoặc null nếu không có quyền.</summary>
    public static string? ScopeOf(string resource, string action) => ScopeOf(Principal, resource, action);

    public static bool Can(string resource, string action) => ScopeOf(Principal, resource, action) != null;

    public static bool CanPolicy(string policy)
    {
        var p = Principal;
        if (p == null) return false;
        if (!AuthorizationPolicies.PolicyRequirements.TryGetValue(policy, out var reqs)) return false; // policy lạ => từ chối
        return reqs.Any(r => ScopeOf(p, r.Resource, r.Action) != null);
    }

    // ---------- Bắt buộc quyền (ném exception) ----------
    public static void Require(string resource, string action)
    {
        var p = Principal;
        if (p == null) throw NotLoggedIn(resource, action);
        if (ScopeOf(p, resource, action) == null) throw NoPermission(p, resource, action);
    }

    public static void RequirePolicy(string policy)
    {
        var p = Principal;
        if (p == null) throw NotLoggedIn("POLICY", policy);
        if (!CanPolicy(policy)) throw NoPermission(p, "POLICY", policy);
    }

    /// <summary>Cần ít nhất MỘT trong các quyền.</summary>
    public static void RequireAny(params (string Resource, string Action)[] options)
    {
        var p = Principal;
        var first = options.Length > 0 ? options[0] : ("?", "?");
        if (p == null) throw NotLoggedIn(first.Item1, first.Item2);
        if (!options.Any(o => ScopeOf(p, o.Resource, o.Action) != null))
            throw NoPermission(p, first.Item1, first.Item2);
    }

    /// <summary>
    /// Kiểm tra theo phạm vi dữ liệu: ALL/PARTIAL => cho qua; OWN => chỉ khi chủ bản ghi là chính người dùng.
    /// </summary>
    public static void RequireOwnerOrAll(string resource, string action, long? ownerId)
    {
        var p = Principal;
        if (p == null) throw NotLoggedIn(resource, action);
        var scope = ScopeOf(p, resource, action);
        if (scope == null) throw NoPermission(p, resource, action);
        if (scope == AuthorizationPolicies.ScopeOwn && (ownerId == null || ownerId != GetUserId(p)))
            throw NoPermission(p, resource, action, "Chỉ được thao tác trên dữ liệu do chính bạn tạo.");
    }

    /// <summary>Chủ bản ghi luôn được phép; người khác phải có quyền (resource, action).</summary>
    public static void RequireSelfOr(long ownerId, string resource, string action)
    {
        var p = Principal;
        if (p == null) throw NotLoggedIn(resource, action);
        if (GetUserId(p) == ownerId) return;
        if (ScopeOf(p, resource, action) == null) throw NoPermission(p, resource, action);
    }

    // ---------- Tạo exception + ghi nhận từ chối ----------
    public static UnauthenticatedException NotLoggedIn(string resource = "?", string action = "?")
    {
        RaiseDenied(null, resource, action, "Chưa đăng nhập / token không hợp lệ");
        return new UnauthenticatedException();
    }

    public static ForbiddenException NoPermission(ClaimsPrincipal? p, string resource, string action, string? detail = null)
    {
        RaiseDenied(GetUserId(p), resource, action, detail ?? "Thiếu quyền");
        return new ForbiddenException(
            detail ?? $"Bạn không có quyền thực hiện thao tác này ({resource}/{action}).", resource, action);
    }

    private static void RaiseDenied(long? userId, string resource, string action, string reason)
    {
        try { Denied?.Invoke(userId, resource, action, reason); }
        catch { /* ghi nhật ký lỗi không được làm hỏng luồng kiểm tra quyền */ }
    }
}