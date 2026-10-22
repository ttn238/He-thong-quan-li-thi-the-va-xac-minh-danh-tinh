namespace DeathManagement.API.Helpers;

/// <summary>
/// Tiện ích phân loại và kiểm soát an toàn hình ảnh tử thi và người mất tích
/// </summary>
public static class ImageHelper
{
    // Các loại hình ảnh
    public const string TypeBodyExamination = "ThiThe_KhamNghiem";
    public const string TypeBodyFace = "ThiThe_KhuonMat";
    public const string TypeBodyFull = "ThiThe_ToanThan";
    public const string TypeMissingPortrait = "MatTich_ChanDung";
    public const string TypeMissingFeatures = "MatTich_DacDiem";
    public const string TypeIncidentVehicle = "HienTruong_Xe";
    public const string TypeIncidentItem = "HienTruong_VatDung";
    public const string TypeVerificationDna = "MinhChung_ADN";

    /// <summary>
    /// Kiểm tra xem hình ảnh có thuộc loại nhạy cảm cần bảo vệ nghiêm ngặt không
    /// </summary>
    public static bool IsSensitiveImage(string imageType, long? bodyId)
    {
        if (bodyId.HasValue && bodyId.Value > 0)
            return true;

        return imageType switch
        {
            TypeBodyExamination or TypeBodyFace or TypeBodyFull => true,
            _ => false
        };
    }


    /// <summary>
    /// Lấy MIME type từ phần mở rộng đã được kiểm tra.
    /// </summary>
    public static string GetMimeType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();

        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".bmp" => "image/bmp",
            _ => throw new InvalidOperationException(
                "Định dạng ảnh không được hỗ trợ.")
        };
    }
}
