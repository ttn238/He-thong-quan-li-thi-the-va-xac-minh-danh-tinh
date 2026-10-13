namespace DeathManagement.API.Entities;

/// <summary>
/// Đại diện cho Hình ảnh liên quan đến hồ sơ thi thể (được đánh dấu nhạy cảm, bảo mật cao)
/// </summary>
public class BodyImage
{
    public long MaHinhAnh { get; set; }
    public string TenFile { get; set; } = string.Empty;
    public string TenGoc { get; set; } = string.Empty;
    public string DuongDan { get; set; } = string.Empty;
    public string LoaiHinhAnh { get; set; } = "ThiThe_KhamNghiem";
    public string DinhDang { get; set; } = "image/jpeg";
    public long KichThuoc { get; set; }
    public int? ChieuRong { get; set; }
    public int? ChieuCao { get; set; }
    public string? HashMD5 { get; set; }

    /// <summary>
    /// Hình ảnh thi thể luôn là ảnh nhạy cảm (LaNhayCam = true)
    /// </summary>
    public bool LaNhayCam { get; set; } = true;

    public string? MoTa { get; set; }
    public long MaHoSoThiThe { get; set; }
    public long MaTaiKhoanTaiLen { get; set; }
    public DateTime ThoiGianTaiLen { get; set; } = DateTime.UtcNow;
    public string TrangThai { get; set; } = "HoatDong";

    // Navigation properties
    public virtual User? Uploader { get; set; }
}
