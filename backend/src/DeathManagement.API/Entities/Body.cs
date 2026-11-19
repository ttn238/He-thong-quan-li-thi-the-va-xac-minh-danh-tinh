namespace DeathManagement.API.Entities;

/// <summary>
/// Ánh xạ bảng HoSoThiThe — hồ sơ thi thể chưa xác định danh tính.
/// </summary>
public class Body
{
    public long MaHoSoThiThe { get; set; }

    /// <summary>Thời điểm phát hiện thi thể.</summary>
    public DateTime ThoiGianPhatHien { get; set; }

    /// <summary>Địa điểm phát hiện.</summary>
    public string? DiaDiem { get; set; }

    /// <summary>Trạng thái hồ sơ theo state machine: MoiTiepNhan, DangLuuGiu, DangXacMinh, DaXacDinh, DaBanGiao, DongHoSo, TiepTucLuuGiu.</summary>
    public string TrangThai { get; set; } = "MoiTiepNhan";

    /// <summary>Giới tính ước tính: Nam, Nu, Khac, KhongXacDinh.</summary>
    public string? GioiTinh { get; set; }

    /// <summary>Tuổi ước lượng (0–130).</summary>
    public short? TuoiUocLuong { get; set; }

    /// <summary>Chiều cao đơn vị cm.</summary>
    public decimal? ChieuCao { get; set; }

    /// <summary>Mô tả đặc điểm cơ thể (vết sẹo, hình xăm, v.v.).</summary>
    public string? DacDiemCoThe { get; set; }

    /// <summary>Vector đặc trưng khuôn mặt (AI face embedding).</summary>
    public byte[]? KhuonMat { get; set; }

    /// <summary>Liên kết Tin báo tương ứng (nếu có).</summary>
    public int? MaTinBao { get; set; }
}
