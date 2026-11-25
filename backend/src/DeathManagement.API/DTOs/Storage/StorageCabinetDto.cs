namespace DeathManagement.API.DTOs.Storage;

/// <summary>
/// Thống kê sức chứa và danh sách ngăn theo Tủ lưu trữ (Task 7.2)
/// </summary>
public class StorageCabinetDto
{
    /// <summary>Tên khu vực</summary>
    public string Khu { get; set; } = string.Empty;

    /// <summary>Tên tủ lưu trữ (ví dụ: Tủ A01, Tủ 1...)</summary>
    public string Tu { get; set; } = string.Empty;

    /// <summary>Tổng số ngăn trong tủ</summary>
    public int TongSoNgan { get; set; }

    /// <summary>Tổng sức chứa của tủ</summary>
    public int TongSucChua { get; set; }

    /// <summary>Số lượng thi thể hiện đang lưu giữ trong tủ</summary>
    public int DangChua { get; set; }

    /// <summary>Số chỗ còn trống trong tủ</summary>
    public int ConTrong => Math.Max(0, TongSucChua - DangChua);

    /// <summary>Tỉ lệ lấp đầy theo phần trăm (0 - 100%)</summary>
    public double TiLeLapDay => TongSucChua > 0 ? Math.Round((double)DangChua / TongSucChua * 100, 1) : 0;

    /// <summary>Cờ đánh dấu tủ đã đầy 100%</summary>
    public bool DaDay => TongSucChua > 0 && DangChua >= TongSucChua;

    /// <summary>Mức độ cảnh báo: BinhThuong, CanhBao, DaDay</summary>
    public string MucDoCanhBao
    {
        get
        {
            if (DaDay) return "DaDay";
            if (TiLeLapDay >= 80) return "CanhBao";
            return "BinhThuong";
        }
    }

    /// <summary>Mã màu hex cảnh báo</summary>
    public string MauCanhBao
    {
        get
        {
            if (DaDay) return "#c0392b";
            if (TiLeLapDay >= 80) return "#d97706";
            return "#1fa971";
        }
    }

    /// <summary>Danh sách các ngăn thuộc tủ</summary>
    public List<StorageLocationDto> DanhSachNgan { get; set; } = new();
}
