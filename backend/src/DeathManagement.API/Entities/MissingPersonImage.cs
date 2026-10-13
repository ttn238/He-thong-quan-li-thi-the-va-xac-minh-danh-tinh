namespace DeathManagement.API.Entities;

/// <summary>
/// Đại diện cho Hình ảnh người mất tích (ảnh chân dung, ảnh vết bớt, hình xăm)
/// </summary>
public class MissingPersonImage
{
    public long MaHinhAnh { get; set; }
    public string TenFile { get; set; } = string.Empty;
    public string TenGoc { get; set; } = string.Empty;
    public string DuongDan { get; set; } = string.Empty;
    public string LoaiHinhAnh { get; set; } = "MatTich_ChanDung";
    public string DinhDang { get; set; } = "image/jpeg";
    public long KichThuoc { get; set; }
    public int? ChieuRong { get; set; }
    public int? ChieuCao { get; set; }
    public string? HashMD5 { get; set; }

    /// <summary>
    /// Ảnh người mất tích thông thường không phải là ảnh nhạy cảm
    /// </summary>
    public bool LaNhayCam { get; set; } = false;

    public string? MoTa { get; set; }
    public long MaHoSoMatTich { get; set; }
    public long MaTaiKhoanTaiLen { get; set; }
    public DateTime ThoiGianTaiLen { get; set; } = DateTime.UtcNow;
    public string TrangThai { get; set; } = "HoatDong";

    // Navigation properties
    public virtual User? Uploader { get; set; }
}
