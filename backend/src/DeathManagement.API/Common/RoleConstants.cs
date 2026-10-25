namespace DeathManagement.API.Common;

/// <summary>
/// Quản lý tập trung các định danh vai trò người dùng trong toàn bộ hệ thống.
/// Sử dụng Mã Code kỹ thuật (ADMIN, CONG_AN, ...) cho mọi kiểm tra bảo mật / phân quyền.
/// </summary>
public static class RoleConstants
{
    // 1. Mã định danh kỹ thuật (Technical Role Codes) - Dùng duy nhất cho logic bảo mật
    public const string Admin = "ADMIN";
    public const string Officer = "CONG_AN";
    public const string Approver = "CONG_AN_DUYET";
    public const string Morgue = "NHA_XAC";
    public const string Citizen = "NGUOI_DAN";

    // Danh sách toàn bộ các mã kỹ thuật hợp lệ
    public static readonly string[] AllCodes = [Admin, Officer, Approver, Morgue, Citizen];

    // Chỉ dùng cho Authorize attribute trên Controller
    public const string AdminRolesCsv = Admin;

    // 2. Tên hiển thị tiếng Việt (Chỉ dùng cho hiển thị giao diện)
    public const string AdminName = "Quản trị hệ thống";
    public const string OfficerName = "Công an / cán bộ xác minh";
    public const string ApproverName = "Công an có thẩm quyền duyệt";
    public const string MorgueName = "Nhân viên nhà xác";
    public const string CitizenName = "Người dân / người thân";

    // 3. Phạm vi dữ liệu phân quyền (PhamVi trong bảng VaiTro_Quyen)
    public static class Scopes
    {
        public const string All = "ALL";         // Toàn quyền trên mọi dữ liệu hệ thống
        public const string Own = "OWN";         // Chỉ dữ liệu do chính người dùng tạo
        public const string Partial = "PARTIAL"; // Dữ liệu phân công hoặc dữ liệu không nhạy cảm

        public static readonly string[] ValidScopes = [All, Own, Partial];
        public static bool IsValid(string? scope) => !string.IsNullOrEmpty(scope) && ValidScopes.Contains(scope);
    }

    /// <summary>
    /// Kiểm tra xem mã code có phải là vai trò quản trị viên hay không.
    /// </summary>
    public static bool IsAdmin(string? roleCode) => string.Equals(roleCode, Admin, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Lấy tên tiếng Việt hiển thị từ mã code.
    /// </summary>
    public static string GetDisplayNameByCode(string? code) => code switch
    {
        Admin => AdminName,
        Officer => OfficerName,
        Approver => ApproverName,
        Morgue => MorgueName,
        Citizen => CitizenName,
        _ => code ?? "Chưa xác định"
    };

    // Helper tương thích ngược với ID cũ nếu cần
    public static string GetCode(int roleId) => roleId switch
    {
        1 => Admin,
        2 => Officer,
        3 => Approver,
        4 => Morgue,
        5 => Citizen,
        _ => "UNKNOWN"
    };

    public static string GetName(int roleId) => roleId switch
    {
        1 => AdminName,
        2 => OfficerName,
        3 => ApproverName,
        4 => MorgueName,
        5 => CitizenName,
        _ => "Khác"
    };
}
