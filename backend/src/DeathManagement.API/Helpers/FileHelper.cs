
using System.Drawing;
using System.Drawing.Imaging;
using System.Security.Cryptography;

namespace DeathManagement.API.Helpers;

/// <summary>
/// Kiểm tra và hỗ trợ xử lý tệp ảnh trước khi lưu vào hệ thống.
/// Chỉ cho phép JPEG, PNG, BMP; tối đa 10 MB mỗi ảnh.
/// </summary>
public static class FileHelper
{
    public const long MaxFileSizeInBytes = 10L * 1024 * 1024;

    public static readonly string[] AllowedImageExtensions =
    {
        ".jpg", ".jpeg", ".png", ".bmp"
    };

    /// <summary>
    /// Kiểm tra phần mở rộng của tệp.
    /// </summary>
    public static bool IsValidImageExtension(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        string extension = Path.GetExtension(fileName).ToLowerInvariant();

        return AllowedImageExtensions.Contains(extension);
    }

    /// <summary>
    /// Kiểm tra dung lượng: lớn hơn 0 và không vượt quá giới hạn.
    /// </summary>
    public static bool IsValidFileSize(
        long fileSizeInBytes,
        long maxBytes = MaxFileSizeInBytes)
    {
        return fileSizeInBytes > 0 && fileSizeInBytes <= maxBytes;
    }

    /// <summary>
    /// Kiểm tra nội dung thực tế có phải ảnh JPEG, PNG hoặc BMP.
    /// Không chỉ dựa vào tên hay phần mở rộng của tệp.
    /// </summary>
    public static bool IsValidImageContent(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return false;

        try
        {
            using var image = Image.FromFile(filePath, useEmbeddedColorManagement: false);

            Guid format = image.RawFormat.Guid;

            return format == ImageFormat.Jpeg.Guid
                || format == ImageFormat.Png.Guid
                || format == ImageFormat.Bmp.Guid;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (OutOfMemoryException)
        {
            // GDI+ cũng có thể ném lỗi này khi tệp không phải ảnh hợp lệ.
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }

    /// <summary>
    /// Tính mã MD5 để đối chiếu tính toàn vẹn của tệp.
    /// MD5 không dùng để xác thực bảo mật.
    /// </summary>
    public static string ComputeMd5Hash(byte[] fileBytes)
    {
        ArgumentNullException.ThrowIfNull(fileBytes);

        return Convert.ToHexStringLower(MD5.HashData(fileBytes));
    }

    /// <summary>
    /// Sinh tên file ngẫu nhiên để tránh ghi đè và lộ tên file gốc.
    /// </summary>
    public static string GenerateSafeFileName(string originalFileName)
    {
        if (!IsValidImageExtension(originalFileName))
            throw new InvalidOperationException("Định dạng ảnh không được hỗ trợ.");

        string extension = Path.GetExtension(originalFileName).ToLowerInvariant();

        return $"{Guid.NewGuid():N}{extension}";
    }
}
