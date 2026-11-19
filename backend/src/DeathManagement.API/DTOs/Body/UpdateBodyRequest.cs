namespace DeathManagement.API.DTOs.Body;

/// <summary>
/// DTO cập nhật thông tin hồ sơ thi thể (BODY:UPDATE).
/// </summary>
public class UpdateBodyRequest
{
    /// <summary>Thời gian phát hiện (không được ở tương lai).</summary>
    public DateTime? ThoiGianPhatHien { get; set; }

    /// <summary>Địa điểm phát hiện.</summary>
    public string? DiaDiem { get; set; }

    /// <summary>Nam | Nu | Khac | KhongXacDinh.</summary>
    public string? GioiTinh { get; set; }

    /// <summary>Tuổi ước lượng (0–130).</summary>
    public short? TuoiUocLuong { get; set; }

    /// <summary>Chiều cao (cm).</summary>
    public decimal? ChieuCao { get; set; }

    /// <summary>Đặc điểm cơ thể.</summary>
    public string? DacDiemCoThe { get; set; }
}
