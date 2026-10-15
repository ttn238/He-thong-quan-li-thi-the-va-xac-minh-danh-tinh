namespace DeathManagement.API.DTOs.Image;

public class UploadImageRequest
{
    public string FilePath { get; set; } = string.Empty;
    public string LoaiHinhAnh { get; set; } = "ThiThe_KhamNghiem";
    public string? MoTa { get; set; }
    public bool LaNhayCam { get; set; } = false;

    // Liên kết đối tượng nghiệp vụ (ít nhất 1 mục)
    public long? MaHoSoThiThe { get; set; }
    public long? MaHoSoMatTich { get; set; }
    public int? MaTinBao { get; set; }
    public long? MaXacMinh { get; set; }

    public long MaTaiKhoanTaiLen { get; set; }
}
