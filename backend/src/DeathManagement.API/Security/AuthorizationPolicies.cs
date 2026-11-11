using System.Globalization;
using System.Text;

namespace DeathManagement.API.Security;

/// <summary>
/// Định nghĩa các chính sách phân quyền RBAC và vai trò chuẩn của hệ thống
/// QUY TẮC PHÂN QUYỀN THỐNG NHẤT (RBAC): vai trò -> (tài nguyên, hành động) -> phạm vi dữ liệu.
/// Ma trận bên dưới phản chiếu đúng bảng VaiTro_Quyen trong database/09_seed_data.sql
/// (kèm bổ sung CONG_AN được tạo tin báo INCIDENT:CREATE).
/// Nguyên tắc: mặc định TỪ CHỐI; chỉ cho phép khi ma trận cho phép.
/// </summary>
public static class AuthorizationPolicies
{
    // Danh sách Vai trò (Roles)
    public const string RoleAdmin = "ADMIN";
    public const string RoleOfficer = "CONG_AN";
    public const string RoleApprover = "CONG_AN_DUYET";
    public const string RoleMorgue = "NHA_XAC";
    public const string RoleCitizen = "NGUOI_DAN";

    public static readonly HashSet<string> AllRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        RoleAdmin,
        RoleOfficer,
        RoleApprover,
        RoleMorgue,
        RoleCitizen
    };
    // Danh sách Chính sách (Policies)
    public const string RequireAdmin = "RequireAdmin";
    public const string RequireOfficer = "RequireOfficer";
    public const string RequireApprover = "RequireApprover";
    public const string RequireOfficerOrAdmin = "RequireOfficerOrAdmin";
    public const string RequireMorgueAccess = "RequireMorgueAccess";
    public const string RequireSensitiveDataViewer = "RequireSensitiveDataViewer";

    // Danh sách Tài nguyên (Resources)
    public const string ResourceAccount = "ACCOUNT";
    public const string ResourcePermission = "PERMISSION";
    public const string ResourceCatalog = "CATALOG";
    public const string ResourceBody = "BODY";
    public const string ResourceMissingPerson = "MISSING_PERSON";
    public const string ResourceIncident = "INCIDENT";
    public const string ResourceAiResult = "AI_RESULT";
    public const string ResourceStorage = "STORAGE";
    public const string ResourceObject = "OBJECT";
    public const string ResourceMatching = "MATCHING";
    public const string ResourceVerification = "VERIFICATION";
    public const string ResourceHandover = "HANDOVER";
    public const string ResourceFile = "FILE";
    public const string ResourceReport = "REPORT";
    public const string ResourceAudit = "AUDIT";

    // Danh sách Hành động (Actions)
    public const string ActionView = "VIEW";
    public const string ActionCreate = "CREATE";
    public const string ActionUpdate = "UPDATE";
    public const string ActionDelete = "DELETE";
    public const string ActionConfirm = "CONFIRM";
    public const string ActionApprove = "APPROVE";
    public const string ActionHandover = "HANDOVER";
    public const string ActionExport = "EXPORT";

    // ===== Phạm vi dữ liệu =====
    public const string ScopeAll = "ALL";         // toàn bộ dữ liệu
    public const string ScopeOwn = "OWN";         // chỉ dữ liệu do chính mình tạo
    public const string ScopePartial = "PARTIAL"; // một phần (được phân công / giới hạn trường)

    public readonly record struct Requirement(string Resource, string Action);

    
    /// Policy -> danh sách yêu cầu (chỉ cần thỏa MỘT yêu cầu trong danh sách).
    public static readonly IReadOnlyDictionary<string, Requirement[]> PolicyRequirements =
        new Dictionary<string, Requirement[]>
        {
            [RequireAdmin] = new[] { new Requirement(ResourcePermission, ActionUpdate) },
            [RequireOfficer] = new[] { new Requirement(ResourceIncident, ActionConfirm) },
            [RequireApprover] = new[] { new Requirement(ResourceVerification, ActionApprove) },
            [RequireOfficerOrAdmin] = new[]
            {
                new Requirement(ResourceIncident, ActionConfirm),
                new Requirement(ResourcePermission, ActionUpdate)
            },
            [RequireMorgueAccess] = new[] { new Requirement(ResourceStorage, ActionView) },
            [RequireSensitiveDataViewer] = new[] { new Requirement(ResourceBody, ActionView) }
        };

    /// <summary>Ma trận: roleCode -> ("RESOURCE:ACTION" -> scope)</summary>
    private static readonly Dictionary<string, Dictionary<string, string>> Matrix = BuildMatrix();

    private static string Key(string resource, string action) => $"{resource}:{action}";

    private static Dictionary<string, Dictionary<string, string>> BuildMatrix()
    {
        var m = new Dictionary<string, Dictionary<string, string>>
        {
            [RoleAdmin] = new(),
            [RoleOfficer] = new(),
            [RoleApprover] = new(),
            [RoleMorgue] = new(),
            [RoleCitizen] = new()
        };

        void Grant(string role, string scope, string res, params string[] acts)
        {
            foreach (var a in acts) m[role][Key(res, a)] = scope;
        }

        const string V = ActionView, C = ActionCreate, U = ActionUpdate, D = ActionDelete;
        const string CF = ActionConfirm, EX = ActionExport, HO = ActionHandover;

        // ---------- ADMIN ----------
        Grant(RoleAdmin, ScopeAll, ResourceAccount, V, C, U);
        Grant(RoleAdmin, ScopeAll, ResourcePermission, V, U);
        Grant(RoleAdmin, ScopeAll, ResourceCatalog, V, C, U);
        Grant(RoleAdmin, ScopeAll, ResourceIncident, V);
        Grant(RoleAdmin, ScopeAll, ResourceFile, V);
        Grant(RoleAdmin, ScopeAll, ResourceAiResult, V);
        Grant(RoleAdmin, ScopeAll, ResourceBody, V, D);
        Grant(RoleAdmin, ScopeAll, ResourceStorage, V, C, U);
        Grant(RoleAdmin, ScopeAll, ResourceMatching, V, U);
        Grant(RoleAdmin, ScopeAll, ResourceVerification, V);
        Grant(RoleAdmin, ScopeAll, ResourceReport, V, EX);
        Grant(RoleAdmin, ScopeAll, ResourceAudit, V);

        // ---------- CONG_AN (cán bộ xác minh) ----------
        Grant(RoleOfficer, ScopeAll, ResourceIncident, V, C, U, CF);
        Grant(RoleOfficer, ScopeAll, ResourceFile, V, C, U);
        Grant(RoleOfficer, ScopeAll, ResourceAiResult, V, CF);
        Grant(RoleOfficer, ScopeAll, ResourceMissingPerson, V, C, U, CF);
        Grant(RoleOfficer, ScopeAll, ResourceBody, V, C, U);
        Grant(RoleOfficer, ScopeAll, ResourceStorage, V, U);
        Grant(RoleOfficer, ScopeAll, ResourceObject, V, C, U);
        Grant(RoleOfficer, ScopeAll, ResourceMatching, V, C);
        Grant(RoleOfficer, ScopeAll, ResourceVerification, V, C, U, CF);
        Grant(RoleOfficer, ScopeAll, ResourceHandover, V, C, U, CF);
        Grant(RoleOfficer, ScopeAll, ResourceReport, V, EX);
        Grant(RoleOfficer, ScopeAll, ResourceAudit, V);

        // ---------- CONG_AN_DUYET = toàn bộ quyền CONG_AN + duyệt kết luận ----------
        foreach (var kv in m[RoleOfficer]) m[RoleApprover][kv.Key] = kv.Value;
        Grant(RoleApprover, ScopeAll, ResourceVerification, ActionApprove);

        // ---------- NHA_XAC ----------
        Grant(RoleMorgue, ScopePartial, ResourceBody, V, U);
        Grant(RoleMorgue, ScopeAll, ResourceStorage, V, C, U);
        Grant(RoleMorgue, ScopeAll, ResourceObject, V, C, U);
        Grant(RoleMorgue, ScopePartial, ResourceFile, V, C);
        Grant(RoleMorgue, ScopeAll, ResourceHandover, V, U, CF, HO);
        Grant(RoleMorgue, ScopePartial, ResourceReport, V, EX);
        Grant(RoleMorgue, ScopePartial, ResourceAudit, V);

        // ---------- NGUOI_DAN ----------
        Grant(RoleCitizen, ScopeOwn, ResourceIncident, V, C, U);
        Grant(RoleCitizen, ScopeOwn, ResourceFile, V, C);
        Grant(RoleCitizen, ScopeOwn, ResourceMissingPerson, V, C, U);

        return m;
    }

    /// <summary>Trả về phạm vi (ALL/OWN/PARTIAL) hoặc null nếu vai trò KHÔNG có quyền.</summary>
    public static string? GetScope(string? roleCode, string resource, string action)
    {
        if (string.IsNullOrEmpty(roleCode)) return null;
        return Matrix.TryGetValue(roleCode, out var perms) && perms.TryGetValue(Key(resource, action), out var scope)
            ? scope
            : null;
    }

    public static bool IsAllowed(string? roleCode, string resource, string action)
        => GetScope(roleCode, resource, action) != null;

    /// <summary>Danh sách claim "RESOURCE:ACTION:SCOPE" gắn vào JWT.</summary>
    public static IEnumerable<string> BuildPermissionClaims(string? roleCode)
    {
        if (string.IsNullOrEmpty(roleCode) || !Matrix.TryGetValue(roleCode, out var perms))
            return Enumerable.Empty<string>();
        return perms.Select(kv => $"{kv.Key}:{kv.Value}").ToList();
    }

    /// <summary>
    /// Đổi tên vai trò đọc từ CSDL (VD "Quản trị hệ thống") sang mã kỹ thuật (VD "ADMIN").
    /// Không nhận diện được -> trả về "" (KHÔNG có quyền nào).
    /// </summary>
    public static string ResolveRoleCode(string? tenVaiTro)
    {
        if (string.IsNullOrWhiteSpace(tenVaiTro)) return string.Empty;

        var s = Normalize(tenVaiTro);                       // "cong an co tham quyen duyet"
        var asCode = s.ToUpperInvariant().Replace(' ', '_'); // "CONG_AN_DUYET" nếu đã là mã
        if (AllRoles.Contains(asCode)) return asCode;

        if (s.Contains("quan tri")) return RoleAdmin;
        if (s.Contains("duyet")) return RoleApprover;
        if (s.Contains("nha xac")) return RoleMorgue;
        if (s.Contains("nguoi dan") || s.Contains("nguoi than")) return RoleCitizen;
        if (s.Contains("cong an") || s.Contains("can bo")) return RoleOfficer;
        return string.Empty;
    }

    private static string Normalize(string input)
    {
        var d = input.Trim().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(d.Length);
        foreach (var ch in d)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
            sb.Append(ch == 'đ' ? 'd' : ch == 'Đ' ? 'D' : ch);
        }
        return sb.ToString().ToLowerInvariant();
    }
}