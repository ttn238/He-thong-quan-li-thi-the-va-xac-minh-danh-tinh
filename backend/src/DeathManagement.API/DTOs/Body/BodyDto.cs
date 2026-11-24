using BodyEntity = DeathManagement.API.Entities.Body;

namespace DeathManagement.API.DTOs.Body;

/// <summary>
/// DTO trả về thông tin hồ sơ thi thể, kèm nhãn hiển thị, mã màu quy chuẩn và danh sách trạng thái kế tiếp hợp lệ.
/// </summary>
public class BodyDto
{
    public long MaHoSoThiThe { get; set; }

    /// <summary>Thời gian phát hiện thi thể.</summary>
    public DateTime ThoiGianPhatHien { get; set; }

    /// <summary>Địa điểm phát hiện.</summary>
    public string? DiaDiem { get; set; }

    /// <summary>Trạng thái hiện tại của hồ sơ.</summary>
    public string TrangThai { get; set; } = BodyEntity.StatusMoiTiepNhan;

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

    /// <summary>Nhãn trạng thái tiếng Việt dùng để hiển thị nhất quán trên UI danh sách và chi tiết.</summary>
    public string NhanTrangThai => BodyEntity.GetStatusLabel(TrangThai);

    /// <summary>Mã màu nền (HEX) của nhãn trạng thái theo quy chuẩn giao diện.</summary>
    public string MauTrangThai => BodyEntity.GetStatusColorHex(TrangThai);

    /// <summary>Mã màu chữ (HEX) của nhãn trạng thái theo quy chuẩn giao diện.</summary>
    public string MauChuTrangThai => BodyEntity.GetStatusTextColorHex(TrangThai);

    /// <summary>Danh sách các trạng thái kế tiếp được phép chuyển sang từ trạng thái hiện tại.</summary>
    public IReadOnlyList<string> TrangThaiTiepTheoHopLe => BodyEntity.GetAllowedNextStatuses(TrangThai);

    /// <summary>Vị trí lưu trữ hiển thị dạng Khu - Tủ - Ngăn.</summary>
    public string ViTriLuuTruHienThi => string.IsNullOrWhiteSpace(Khu) ? "Chưa xếp vị trí" : $"{Khu} - {Tu} - {Ngan}";

    /// <summary>Nhãn giới tính dùng để hiển thị trên UI.</summary>
    public string NhanGioiTinh => GioiTinh switch
    {
        "Nam"           => "Nam",
        "Nu"            => "Nữ",
        "Khac"          => "Khác",
        "KhongXacDinh"  => "Không xác định",
        _               => GioiTinh ?? "Không xác định"
    };
}
