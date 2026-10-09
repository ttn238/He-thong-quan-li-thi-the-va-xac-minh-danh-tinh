using System.Security.Cryptography;
using System.Text;

namespace DeathManagement.API.Security;

public static class PasswordHasher
{
    public static string Hash(string matKhau) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(matKhau)));

    public static bool Verify(string matKhau, string hashDaLuu)
    {
        var a = Encoding.UTF8.GetBytes(Hash(matKhau));
        var b = Encoding.UTF8.GetBytes(hashDaLuu.ToUpperInvariant());
        return CryptographicOperations.FixedTimeEquals(a, b);
    }
}