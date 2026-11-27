namespace DeathManagement.API.Entities;

/// <summary>
/// Ánh xạ bảng NhatKyHeThong — lưu nhật ký thao tác và lịch sử thay đổi trạng thái hồ sơ.
/// </summary>
public class AuditLog
{
    public long MaNhatKy { get; set; }
    public string HanhDong { get; set; } = string.Empty;
    public DateTime ThoiGian { get; set; } = DateTime.Now;
    public string? DoiTuongTacDong { get; set; }
    public long? MaTaiKhoan { get; set; }
    public User? TaiKhoan { get; set; }
}

