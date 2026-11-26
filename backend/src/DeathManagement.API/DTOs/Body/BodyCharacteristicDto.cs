using DeathManagement.API.Entities;

namespace DeathManagement.API.DTOs.Body;

/// <summary>
/// DTO hiển thị một đặc điểm nhận dạng gắn với hồ sơ thi thể (Task 6.7).
/// </summary>
public class BodyCharacteristicDto
{
    public long MaDacDiem { get; set; }
    public long MaHoSoThiThe { get; set; }
    public string LoaiDacDiem { get; set; } = BodyCharacteristic.TypeKhac;
    public string NhanLoaiDacDiem => BodyCharacteristic.GetTypeLabel(LoaiDacDiem);
    public string? ViTriCoThe { get; set; }
    public string MoTa { get; set; } = string.Empty;
    public DateTime NgayGhiNhan { get; set; }
}

/// <summary>
/// DTO dùng cho thao tác thêm mới hoặc cập nhật một đặc điểm nhận dạng (Task 6.7).
/// </summary>
public class SaveBodyCharacteristicRequest
{
    /// <summary>Seo | HinhXam | VetBot | Rang | DiTat | TocDa | TrangPhuc | Khac.</summary>
    public string LoaiDacDiem { get; set; } = string.Empty;

    /// <summary>Vị trí trên cơ thể (tối đa 150 ký tự, có thể để trống).</summary>
    public string? ViTriCoThe { get; set; }

    /// <summary>Mô tả chi tiết đặc điểm nhận dạng (bắt buộc, tối đa 500 ký tự).</summary>
    public string MoTa { get; set; } = string.Empty;

    /// <summary>Ngày giờ ghi nhận (không được ở tương lai; mặc định là thời điểm hiện tại nếu không truyền).</summary>
    public DateTime? NgayGhiNhan { get; set; }
}
