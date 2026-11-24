namespace DeathManagement.API.DTOs.Body;

/// <summary>
/// DTO trả về thông tin hồ sơ thi thể.
/// </summary>
public class BodyDto
{
    public long MaHoSoThiThe { get; set; }

    /// <summary>Thời gian phát hiện thi thể.</summary>
    public DateTime ThoiGianPhatHien { get; set; }

    /// <summary>Địa điểm phát hiện.</summary>
    public string? DiaDiem { get; set; }

    /// <summary>Trạng thái hiện tại của hồ sơ.</summary>
    public string TrangThai { get; set; } = "MoiTiepNhan";

    /// <summary>Giới tính ước tính.</summary>
    public string? GioiTinh { get; set; }

    /// <summary>Tuổi ước lượng.</summary>
    public short? TuoiUocLuong { get; set; }

    /// <summary>Chiều cao (cm).</summary>
    public decimal? ChieuCao { get; set; }

    /// <summary>Đặc điểm cơ thể.</summary>
    public string? DacDiemCoThe { get; set; }

    /// <summary>Mã tin báo liên kết (nếu có).</summary>
    public int? MaTinBao { get; set; }
    public int? MaViTri { get; set; }
    public string? Khu { get; set; }
    public string? Tu { get; set; }
    public string? Ngan { get; set; }

    /// <summary>Nhãn trạng thái dùng để hiển thị trên UI.</summary>
    public string NhanTrangThai => TrangThai switch
    {
        "MoiTiepNhan"   => "Mới tiếp nhận",
        "DangLuuGiu"    => "Đang lưu giữ",
        "DangXacMinh"   => "Đang xác minh",
        "DaXacDinh"     => "Đã xác định",
        "DaBanGiao"     => "Đã bàn giao",
        "DongHoSo"      => "Đóng hồ sơ",
        "TiepTucLuuGiu" => "Tiếp tục lưu giữ",
        _               => TrangThai
    };

    /// <summary>Nhãn giới tính dùng để hiển thị trên UI.</summary>
    public string NhanGioiTinh => GioiTinh switch
    {
        "Nam"            => "Nam",
        "Nu"             => "Nữ",
        "Khac"           => "Khác",
        "KhongXacDinh"  => "Không xác định",
        _               => GioiTinh ?? "Không xác định"
    };
}
