namespace DeathManagement.API.DTOs.Auth;

/// <param name="RoleCode">Mã vai trò kỹ thuật: ADMIN, CONG_AN, CONG_AN_DUYET, NHA_XAC, NGUOI_DAN</param>
/// <param name="Token">JWT mang danh tính + vai trò + quyền; mọi kiểm tra quyền ở backend dựa trên token này</param>
public record LoginResponse(
    long MaTaiKhoan,
    string TenDangNhap,
    string HoTen,
    string TenVaiTro,
    string? DonVi,
    string RoleCode = "",
    string Token = "");