namespace DeathManagement.API.DTOs.Body;

public class BodyFilterRequest
{
    public string? TuKhoa { get; set; }
    public string? TrangThai { get; set; }
    public string? GioiTinh { get; set; }
    public DateTime? TuNgay { get; set; }
    public DateTime? DenNgay { get; set; }
    public int Trang { get; set; } = 1;
    public int SoLuong { get; set; } = 20;
}
