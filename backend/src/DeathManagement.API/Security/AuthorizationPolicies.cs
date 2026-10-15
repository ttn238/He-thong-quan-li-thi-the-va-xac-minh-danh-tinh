namespace DeathManagement.API.Security;

/// <summary>
/// Định nghĩa các chính sách phân quyền RBAC và vai trò chuẩn của hệ thống
/// </summary>
public static class AuthorizationPolicies
{
    // Danh sách Vai trò (Roles)
    public const string RoleAdmin = "ADMIN";
    public const string RoleOfficer = "CONG_AN";
    public const string RoleApprover = "CONG_AN_DUYET";
    public const string RoleMorgue = "NHA_XAC";
    public const string RoleCitizen = "NGUOI_DAN";

    // Danh sách Chính sách (Policies)
    public const string RequireAdmin = "RequireAdmin";
    public const string RequireOfficer = "RequireOfficer";
    public const string RequireApprover = "RequireApprover";
    public const string RequireOfficerOrAdmin = "RequireOfficerOrAdmin";
    public const string RequireMorgueAccess = "RequireMorgueAccess";
    public const string RequireSensitiveDataViewer = "RequireSensitiveDataViewer";

    // Danh sách Tài nguyên (Resources)
    public const string ResourceAccount = "ACCOUNT";
    public const string ResourceBody = "BODY";
    public const string ResourceMissingPerson = "MISSING_PERSON";
    public const string ResourceIncident = "INCIDENT";
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
}
