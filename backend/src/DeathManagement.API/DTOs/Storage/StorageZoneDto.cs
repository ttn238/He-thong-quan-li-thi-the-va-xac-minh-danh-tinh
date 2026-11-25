namespace DeathManagement.API.DTOs.Storage;

/// <summary>
/// Thống kê sức chứa và tình trạng của một Khu lưu trữ (Task 7.2)
/// </summary>
public class StorageZoneDto
{
    /// <summary>Tên khu vực (ví dụ: Khu A, Khu B...)</summary>
    public string Khu { get; set; } = string.Empty;

    /// <summary>Tổng số tủ lưu trữ trong khu</summary>
    public int TongSoTu { get; set; }

    /// <summary>Tổng số ngăn chứa trong khu</summary>
    public int TongSoNgan { get; set; }

    /// <summary>Tổng sức chứa tối đa của khu (tổng số chỗ)</summary>
    public int TongSucChua { get; set; }

    /// <summary>Số lượng thi thể hiện đang lưu giữ trong khu</summary>
    public int DangChua { get; set; }

    /// <summary>Số chỗ còn trống trong khu</summary>
    public int ConTrong => Math.Max(0, TongSucChua - DangChua);

    /// <summary>Tỉ lệ lấp đầy theo phần trăm (0 - 100%)</summary>
    public double TiLeLapDay => TongSucChua > 0 ? Math.Round((double)DangChua / TongSucChua * 100, 1) : 0;

    /// <summary>Cờ đánh dấu khu vực đã đầy 100% (không còn ngăn trống)</summary>
    public bool DaDay => TongSucChua > 0 && DangChua >= TongSucChua;

    /// <summary>
    /// Mức độ cảnh báo: BinhThuong (< 80%), CanhBao (80% - < 100%), DaDay (>= 100%)
    /// </summary>
    public string MucDoCanhBao
    {
        get
        {
            if (DaDay) return "DaDay";
            if (TiLeLapDay >= 80) return "CanhBao";
            return "BinhThuong";
        }
    }

    /// <summary>Thông báo cảnh báo hiển thị cho người dùng</summary>
    public string CanhBao
    {
        get
        {
            if (DaDay) return $"⛔ CẢNH BÁO: {Khu} đã đầy 100% ({DangChua}/{TongSucChua})! Không còn ngăn trống.";
            if (TiLeLapDay >= 80) return $"⚠️ CẢNH BÁO: {Khu} sắp đầy ({TiLeLapDay}% - còn {ConTrong} chỗ)!";
            return $"✅ {Khu} hoạt động bình thường (còn {ConTrong} chỗ trống).";
        }
    }

    /// <summary>Mã màu hex cảnh báo: Đỏ (#c0392b) khi đầy 100%, Vàng (#d97706) khi >= 80%, Xanh (#1fa971) khi bình thường</summary>
    public string MauCanhBao
    {
        get
        {
            if (DaDay) return "#c0392b";
            if (TiLeLapDay >= 80) return "#d97706";
            return "#1fa971";
        }
    }

    /// <summary>Danh sách tên các tủ lưu trữ trong khu</summary>
    public List<string> DanhSachTu { get; set; } = new();
}
