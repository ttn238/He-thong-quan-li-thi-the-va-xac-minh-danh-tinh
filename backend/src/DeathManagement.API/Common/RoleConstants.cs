namespace DeathManagement.API.Common;

/// <summary>
/// Quản lý tập trung các định danh vai trò người dùng trong toàn bộ hệ thống.
/// Tránh việc viết chuỗi ký tự rải rác (magic strings) gây lỗi khi bảo trì.
/// </summary>
public static class RoleConstants
{
    // 1. Mã định danh kỹ thuật (Technical Role Codes)
    public const string Admin = "ADMIN";
    public const string Officer = "CONG_AN";
    public const string Approver = "CONG_AN_DUYET";
    public const string Morgue = "NHA_XAC";
    public const string Citizen = "NGUOI_DAN";

    // 2. Mã khóa chính trong CSDL (Khớp 100% với dữ liệu 09_seed_data.sql)
    public const int AdminId = 1;
    public const int OfficerId = 2;
    public const int ApproverId = 3;
    public const int MorgueId = 4;
    public const int CitizenId = 5;

    // 3. Tên hiển thị tiếng Việt (Tên thân thiện trên giao diện)
    public const string AdminName = "Quản trị hệ thống";
    public const string OfficerName = "Công an / cán bộ xác minh";
    public const string ApproverName = "Công an có thẩm quyền duyệt";
    public const string MorgueName = "Nhân viên nhà xác";
    public const string CitizenName = "Người dân / người thân";

    // Nhóm vai trò cho phép gọi các API phân quyền của Quản trị viên
    public const string AdminRolesCsv = "ADMIN,Quản trị hệ thống,QuanTriVien";

    // Danh sách toàn bộ 5 ID vai trò hợp lệ
    public static readonly int[] AllRoleIds = [AdminId, OfficerId, ApproverId, MorgueId, CitizenId];

    // Phạm vi dữ liệu phân quyền (PhamVi trong bảng VaiTro_Quyen)
    public static class Scopes
    {
        public const string All = "ALL";         // Toàn quyền trên mọi dữ liệu hệ thống
        public const string Own = "OWN";         // Chỉ dữ liệu do chính người dùng tạo
        public const string Partial = "PARTIAL"; // Dữ liệu phân công hoặc dữ liệu không nhạy cảm
    }

    /// <summary>
    /// Kiểm tra mã vai trò có tồn tại hợp lệ trong hệ thống hay không (1 đến 5).
    /// </summary>
    public static bool IsValidRoleId(int roleId) => roleId is >= 1 and <= 5;

    /// <summary>
    /// Lấy mã kỹ thuật (ADMIN, CONG_AN, ...) từ ID vai trò.
    /// </summary>
    public static string GetCode(int roleId) => roleId switch
    {
        AdminId => Admin,
        OfficerId => Officer,
        ApproverId => Approver,
        MorgueId => Morgue,
        CitizenId => Citizen,
        _ => "UNKNOWN"
    };

    /// <summary>
    /// Lấy tên tiếng Việt hiển thị từ ID vai trò.
    /// </summary>
    public static string GetName(int roleId) => roleId switch
    {
        AdminId => AdminName,
        OfficerId => OfficerName,
        ApproverId => ApproverName,
        MorgueId => MorgueName,
        CitizenId => CitizenName,
        _ => "Khác"
    };
}
