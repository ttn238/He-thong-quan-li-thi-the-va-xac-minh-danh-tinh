using System.Security.Cryptography;

namespace DeathManagement.API.Helpers;

/// <summary>
/// Tiện ích xử lý tệp tin và lưu trữ an toàn
/// </summary>
public static class FileHelper
{
    private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxFileSizeInBytes = 10 * 1024 * 1024; // 10 MB

    /// <summary>
    /// Kiểm tra phần mở rộng tệp có hợp lệ là ảnh hay không
    /// </summary>
    public static bool IsValidImageExtension(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return AllowedImageExtensions.Contains(ext);
    }

    /// <summary>
    /// Kiểm tra kích thước tệp có vượt quá dung lượng tối đa cho phép
    /// </summary>
    public static bool IsValidFileSize(long fileSizeInBytes, long maxBytes = MaxFileSizeInBytes)
    {
        return fileSizeInBytes > 0 && fileSizeInBytes <= maxBytes;
    }

    /// <summary>
    /// Tính toán mã băm MD5 của mảng byte tệp để kiểm tra toàn vẹn và chống trùng lặp
    /// </summary>
    public static string ComputeMd5Hash(byte[] fileBytes)
    {
        byte[] hash = MD5.HashData(fileBytes);
        return Convert.ToHexStringLower(hash);
    }

    /// <summary>
    /// Sinh tên tệp an toàn dạng GUID ngẫu nhiên giữ lại phần mở rộng gốc
    /// </summary>
    public static string GenerateSafeFileName(string originalFileName)
    {
        var ext = Path.GetExtension(originalFileName).ToLowerInvariant();
        return $"{Guid.NewGuid():N}{ext}";
    }
}
