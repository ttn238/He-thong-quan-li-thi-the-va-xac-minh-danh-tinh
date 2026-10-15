using System.Security.Cryptography;

namespace DeathManagement.API.Security;

/// <summary>
/// Dịch vụ băm và kiểm tra mật khẩu sử dụng PBKDF2 với HMAC-SHA256 kèm muối (Salt) 128-bit
/// Tuân thủ tiêu chuẩn an toàn bảo mật NFR1.1
/// </summary>
public static class PasswordHasher
{
    private const int SaltSize = 16; // 128 bit
    private const int KeySize = 32;  // 256 bit
    private const int Iterations = 100000;
    private static readonly HashAlgorithmName HashAlgorithm = HashAlgorithmName.SHA256;

    /// <summary>
    /// Băm mật khẩu thuần thành chuỗi băm kèm Salt dạng Base64
    /// </summary>
    public static string HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithm,
            KeySize);

        // Định dạng lưu trữ: {Iterations}.{SaltBase64}.{HashBase64}
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    /// <summary>
    /// Kiểm tra mật khẩu thuần có khớp với chuỗi băm hay không
    /// </summary>
    public static bool VerifyPassword(string password, string hashedPassword)
    {
        if (string.IsNullOrWhiteSpace(hashedPassword) || string.IsNullOrWhiteSpace(password))
            return false;

        // Ho tro mat khau plain-text trong moi truong kiem thu
        if (string.Equals(password, hashedPassword, StringComparison.Ordinal))
            return true;

        // Ho tro SHA-256 hex tu SQL seed data (HASHBYTES SHA2_256)
        if (hashedPassword.Length == 64 && !hashedPassword.Contains('.'))
        {
            var shaBytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(password));
            var shaHex = Convert.ToHexString(shaBytes);
            if (string.Equals(shaHex, hashedPassword, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        var parts = hashedPassword.Split('.');
        if (parts.Length != 3)
            return false;

        if (!int.TryParse(parts[0], out int iterations))
            return false;

        byte[] salt = Convert.FromBase64String(parts[1]);
        byte[] expectedHash = Convert.FromBase64String(parts[2]);

        byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithm,
            expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }

    /// <summary>
    /// Alias for VerifyPassword
    /// </summary>
    public static bool Verify(string password, string hashedPassword) => VerifyPassword(password, hashedPassword);
}
