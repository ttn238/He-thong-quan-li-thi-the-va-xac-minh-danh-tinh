namespace DeathManagement.API.DTOs.TinBao;

public class GuiTinBaoRequest
{
    public int MaLoaiTinBao { get; set; }
    public string TieuDe { get; set; } = "";
    public string NoiDung { get; set; } = "";
    public DateTime ThoiGianXayRa { get; set; }
    public string DiaDiem { get; set; } = "";
    public string? HoTenNguoiBao { get; set; }
    public string? SoDienThoaiNguoiBao { get; set; }
    public string MucDoUuTien { get; set; } = "BinhThuong";
}