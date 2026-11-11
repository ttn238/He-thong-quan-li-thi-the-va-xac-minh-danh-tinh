using DeathManagement.API.DTOs.Auth;

namespace DeathManagement.API;

public static class Session
{
    public static LoginResponse? TaiKhoan { get; set; }
    public static string DonVi { get; set; } = "";

    /// <summary>JWT của phiên hiện tại (null nếu chưa đăng nhập)</summary>
    public static string? Token => TaiKhoan?.Token;

    public static void Clear()
    {
        TaiKhoan = null;
        DonVi = "";
    }
}