namespace DeathManagement.API.Entities;

/// <summary>
/// Đại diện cho Tệp hình ảnh và đa phương tiện đính kèm trong hệ thống (Bảng HinhAnh - Mục 3.7)
/// </summary>
public class Image
{
    public long MaHinhAnh { get; set; }
    public string TenFile { get; set; } = string.Empty;
    public string TenGoc { get; set; } = string.Empty;
    public string DuongDan { get; set; } = string.Empty;
    public string LoaiHinhAnh { get; set; } = string.Empty;
    public string DinhDang { get; set; } = "image/jpeg";
    public long KichThuoc { get; set; }
    public int? ChieuRong { get; set; }
    public int? ChieuCao { get; set; }
    public string? HashMD5 { get; set; }
    public bool LaNhayCam { get; set; } = false;
    public string? MoTa { get; set; }

    // Khóa ngoại trỏ đến các đối tượng nghiệp vụ
    public long? MaHoSoThiThe { get; set; }
    public long? MaHoSoMatTich { get; set; }
    public int? MaTinBao { get; set; }
    public long? MaXacMinh { get; set; }

    public long MaTaiKhoanTaiLen { get; set; }
    public DateTime ThoiGianTaiLen { get; set; } = DateTime.UtcNow;
    public string TrangThai { get; set; } = "HoatDong"; // HoatDong, DaXoa, An

    // Navigation property
    public virtual User? TaiKhoanTaiLen { get; set; }
}
