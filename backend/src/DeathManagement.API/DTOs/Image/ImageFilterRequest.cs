namespace DeathManagement.API.DTOs.Image;

public class ImageFilterRequest
{
    public string? Keyword { get; set; }
    public string? LoaiHinhAnh { get; set; }
    public bool? ChiHienNhayCam { get; set; }
    public long? MaHoSoThiThe { get; set; }
    public long? MaHoSoMatTich { get; set; }
    public int? MaTinBao { get; set; }
    public long? MaXacMinh { get; set; }
    public bool? ChiHoSoThiThe { get; set; }
    public bool? ChiHoSoMatTich { get; set; }
}
