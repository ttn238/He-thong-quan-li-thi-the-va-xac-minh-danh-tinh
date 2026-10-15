using DeathManagement.API.DTOs.Auth;

namespace DeathManagement.API;

public static class Session
{
    public static LoginResponse? TaiKhoan { get; set; }
    public static string DonVi { get; set; } = "";

    public static void Clear()
    {
        TaiKhoan = null;
        DonVi = "";
    }
}