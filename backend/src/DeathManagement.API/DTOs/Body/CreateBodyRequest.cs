using System.ComponentModel.DataAnnotations;

namespace DeathManagement.API.DTOs.Body;

/// <summary>
/// DTO tạo hồ sơ thi thể mới — gọi sp_LapHoSoThiThe.
/// </summary>
public class CreateBodyRequest
{
    /// <summary>Thời gian phát hiện thi thể (không được ở tương lai).</summary>
    [Required(ErrorMessage = "Vui lòng nhập thời gian phát hiện.")]
    public DateTime ThoiGianPhatHien { get; set; }

    /// <summary>Địa điểm phát hiện.</summary>
    [Required(ErrorMessage = "Vui lòng nhập địa điểm.")]
    [MaxLength(255, ErrorMessage = "Địa điểm không được vượt quá 255 ký tự.")]
    public string DiaDiem { get; set; } = string.Empty;

    /// <summary>Nam | Nu | Khac | KhongXacDinh.</summary>
    [MaxLength(20)]
    public string GioiTinh { get; set; } = "KhongXacDinh";

    /// <summary>Tuổi ước lượng (0–130), để trống nếu không xác định.</summary>
    [Range(0, 130, ErrorMessage = "Tuổi ước lượng phải từ 0 đến 130.")]
    public short? TuoiUocLuong { get; set; }

    /// <summary>Chiều cao (cm), phải lớn hơn 0 nếu có.</summary>
    [Range(0.01, 300, ErrorMessage = "Chiều cao phải lớn hơn 0.")]
    public decimal? ChieuCao { get; set; }

    /// <summary>Đặc điểm cơ thể: vết sẹo, hình xăm, dị tật, trang phục, v.v.</summary>
    public string? DacDiemCoThe { get; set; }

    /// <summary>Liên kết Tin báo nếu hồ sơ được lập từ một tin báo có sẵn.</summary>
    public int? MaTinBao { get; set; }
}
