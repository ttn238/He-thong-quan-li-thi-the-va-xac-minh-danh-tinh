namespace DeathManagement.API.DTOs.Storage;

/// <summary>
/// DTO biểu diễn một mốc thời gian trong lịch sử di chuyển / lưu trữ của hồ sơ thi thể (Task 7.7).
/// </summary>
public class StorageMovementHistoryDto
{
    public long MaLichSu { get; set; }
    public long MaHoSoThiThe { get; set; }
    public int MaViTri { get; set; }
    public string Khu { get; set; } = string.Empty;
    public string Tu { get; set; } = string.Empty;
    public string Ngan { get; set; } = string.Empty;
    public string ViTriDayDu => $"{Khu} - {Tu} - {Ngan}";

    /// <summary>Thời điểm thi thể được xếp vào vị trí này.</summary>
    public DateTime ThoiGianVao { get; set; }

    /// <summary>Thời điểm thi thể được chuyển sang vị trí kế tiếp (null nếu đang lưu tại vị trí hiện tại).</summary>
    public DateTime? ThoiGianRa { get; set; }

    public long? MaTaiKhoan { get; set; }

    /// <summary>Tên cán bộ / nhân viên thực hiện thao tác.</summary>
    public string NguoiThaoTac { get; set; } = "Hệ thống";

    /// <summary>Lý do xếp hoặc điều chuyển vị trí lưu trữ.</summary>
    public string LyDo { get; set; } = string.Empty;

    /// <summary>Cờ đánh dấu vị trí lưu trữ hiện tại đang hoạt động (Active badge).</summary>
    public bool LaViTriHienTai { get; set; }
}
