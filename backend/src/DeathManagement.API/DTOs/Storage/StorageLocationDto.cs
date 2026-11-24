namespace DeathManagement.API.DTOs.Storage;

public class StorageLocationDto
{
    public int MaViTri { get; set; }
    public string Khu { get; set; } = string.Empty;
    public string Tu { get; set; } = string.Empty;
    public string Ngan { get; set; } = string.Empty;
    public int SucChua { get; set; } = 1;

    /// <summary>
    /// Ký hiệu hiển thị thân thiện: ví dụ "Khu A - Tủ A01 - Ngăn N01"
    /// </summary>
    public string ViTriDayDu => $"{Khu} - {Tu} - {Ngan}";
}
