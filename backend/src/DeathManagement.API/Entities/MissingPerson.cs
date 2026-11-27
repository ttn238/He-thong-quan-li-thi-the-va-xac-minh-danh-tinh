namespace DeathManagement.API.Entities;

/// <summary>
/// Ánh xạ bảng HoSoNguoiMatTich — hồ sơ người mất tích (Epic 8).
/// </summary>
public class MissingPerson
{
    public const string StatusDangTimKiem = "DangTimKiem";
    public const string StatusDaXacDinh   = "DaXacDinh";
    public const string StatusDaDong      = "DaDong";

    public static readonly IReadOnlyList<string> ValidStatuses =
    [
        StatusDangTimKiem,
        StatusDaXacDinh,
        StatusDaDong
    ];

    public long MaHoSoMatTich { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public DateTime? NgaySinh { get; set; }
    public string? GioiTinh { get; set; }
    public DateTime? ThoiGianMatTich { get; set; }
    public string? NoiMatTich { get; set; }
    public string TrangThai { get; set; } = StatusDangTimKiem;

    public ICollection<Relative> NguoiThans { get; set; } = new List<Relative>();

    public static string GetStatusLabel(string? status) => status switch
    {
        StatusDangTimKiem => "Đang tìm kiếm",
        StatusDaXacDinh   => "Đã xác định",
        StatusDaDong      => "Đã đóng",
        _                 => status ?? "Không xác định"
    };

    public static string GetStatusColorHex(string? status) => status switch
    {
        StatusDangTimKiem => "#FCF3CF",
        StatusDaXacDinh   => "#D5F5E3",
        StatusDaDong      => "#E5E7E9",
        _                 => "#F2F4F4"
    };

    public static string GetStatusTextColorHex(string? status) => status switch
    {
        StatusDangTimKiem => "#7D6608",
        StatusDaXacDinh   => "#145A32",
        StatusDaDong      => "#424949",
        _                 => "#1E293B"
    };

    public static string GetGenderLabel(string? gender) => gender switch
    {
        "Nam"  => "Nam",
        "Nu"   => "Nữ",
        "Khac" => "Khác",
        _      => string.IsNullOrWhiteSpace(gender) ? "Không xác định" : gender
    };
}

/// <summary>
/// Ánh xạ bảng NguoiThan — thông tin người thân liên hệ của hồ sơ người mất tích.
/// </summary>
public class Relative
{
    public long MaNguoiThan { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public string? QuanHe { get; set; }
    public string? SoDienThoai { get; set; }
    public string? DiaChi { get; set; }
    public long MaHoSoMatTich { get; set; }

    public MissingPerson? HoSoMatTich { get; set; }
}
