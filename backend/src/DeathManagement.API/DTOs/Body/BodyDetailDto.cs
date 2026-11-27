namespace DeathManagement.API.DTOs.Body;

/// <summary>
/// DTO tổng hợp đầy đủ chi tiết hồ sơ thi thể (Task 6.3 — Xem chi tiết thi thể).
/// Kế thừa BodyDto để tương thích ngược với API và các màn hình hiện có.
/// </summary>
public class BodyDetailDto : BodyDto
{
    /// <summary>Thông tin tin báo gốc liên kết (ẩn với vai trò NHA_XAC).</summary>
    public BodyIncidentSummaryDto? TinBao { get; set; }

    /// <summary>Vị trí ngăn lạnh hiện tại từ view v_ViTriHienTai kết hợp ViTriLuuTru.</summary>
    public BodyStorageCurrentDto? ViTriHienTai { get; set; }

    /// <summary>Lịch sử các lần xếp / điều chuyển vị trí lưu trữ.</summary>
    public List<BodyStorageHistoryDto> LichSuLuuTru { get; set; } = new();

    /// <summary>Danh sách đồ vật cá nhân kèm theo thi thể (DoVatCaNhan + LoaiDoVat).</summary>
    public List<BodyPersonalItemDto> DoVatCaNhan { get; set; } = new();

    /// <summary>Danh sách hình ảnh lấy từ sp_LayHinhAnhTheoHoSo (có kiểm soát ảnh nhạy cảm).</summary>
    public List<BodyImageItemDto> HinhAnh { get; set; } = new();

    /// <summary>Danh sách kết quả đối sánh với hồ sơ người mất tích (v_DoiSanhChiTiet).</summary>
    public List<BodyMatchResultDto> KetQuaDoiSanh { get; set; } = new();

    /// <summary>Lịch sử thay đổi trạng thái hồ sơ từ NhatKyHeThong.</summary>
    public List<BodyStatusHistoryDto> LichSuTrangThai { get; set; } = new();

    /// <summary>Đánh dấu người xem đang ở chế độ hạn chế (vai trò NHA_XAC / phạm vi PARTIAL).</summary>
    public bool CheDoHanCheNhaXac { get; set; }

    /// <summary>Người xem có quyền xem thông tin đồ vật cá nhân hay không.</summary>
    public bool CoQuyenXemDoVat { get; set; }

    /// <summary>Người xem có quyền xem hình ảnh của hồ sơ hay không.</summary>
    public bool CoQuyenXemHinhAnh { get; set; }

    /// <summary>Người xem có quyền xem kết quả đối sánh hay không.</summary>
    public bool CoQuyenXemDoiSanh { get; set; }
}

/// <summary>Tóm tắt thông tin Tin báo gốc liên kết với hồ sơ thi thể.</summary>
public class BodyIncidentSummaryDto
{
    public int MaTinBao { get; set; }
    public string MaTin { get; set; } = string.Empty;
    public string? TenLoaiTinBao { get; set; }
    public string TieuDe { get; set; } = string.Empty;
    public string NoiDung { get; set; } = string.Empty;
    public DateTime ThoiGianXayRa { get; set; }
    public string DiaDiem { get; set; } = string.Empty;
    public string NguonTin { get; set; } = string.Empty;
    public string? HoTenNguoiBao { get; set; }
    public string? SoDienThoaiNguoiBao { get; set; }
    public string? EmailNguoiBao { get; set; }
    public string MucDoUuTien { get; set; } = string.Empty;
    public string TrangThai { get; set; } = string.Empty;
    public string? GhiChuXuLy { get; set; }
    public DateTime NgayTiepNhan { get; set; }

    public string NhanNguonTin => NguonTin switch
    {
        "NguoiDan" => "Người dân",
        "CanBo" => "Cán bộ",
        _ => NguonTin
    };

    public string NhanMucDoUuTien => MucDoUuTien switch
    {
        "Thap" => "Thấp",
        "BinhThuong" => "Bình thường",
        "Cao" => "Cao",
        "KhanCap" => "Khẩn cấp",
        _ => MucDoUuTien
    };

    public string NhanTrangThai => TrangThai switch
    {
        "MoiTiepNhan" => "Mới tiếp nhận",
        "DaTiepNhan" => "Đã tiếp nhận",
        "YeuCauBoSung" => "Yêu cầu bổ sung",
        "TuChoi" => "Từ chối",
        "DangXacMinh" => "Đang xác minh",
        "DaXuLy" => "Đã xử lý",
        _ => TrangThai
    };
}

/// <summary>Thông tin vị trí lưu trữ hiện tại của thi thể (v_ViTriHienTai + ViTriLuuTru).</summary>
public class BodyStorageCurrentDto
{
    public int MaViTri { get; set; }
    public string Khu { get; set; } = string.Empty;
    public string Tu { get; set; } = string.Empty;
    public string Ngan { get; set; } = string.Empty;
    public int SucChua { get; set; }
    public DateTime ThoiGianXep { get; set; }

    public string ViTriHienThi => $"{Khu} - {Tu} - {Ngan}";
}

/// <summary>Một mốc trong lịch sử xếp / di chuyển vị trí lưu trữ (LichSuLuuTru + ViTriLuuTru).</summary>
public class BodyStorageHistoryDto
{
    public long MaLichSu { get; set; }
    public DateTime ThoiGian { get; set; }
    public int MaViTri { get; set; }
    public string Khu { get; set; } = string.Empty;
    public string Tu { get; set; } = string.Empty;
    public string Ngan { get; set; } = string.Empty;

    public string ViTriHienThi => $"{Khu} - {Tu} - {Ngan}";
}

/// <summary>Đồ vật cá nhân kèm theo hồ sơ thi thể (DoVatCaNhan + LoaiDoVat).</summary>
public class BodyPersonalItemDto
{
    public long MaDoVat { get; set; }
    public string Ten { get; set; } = string.Empty;
    public int MaLoaiDoVat { get; set; }
    public string TenLoaiDoVat { get; set; } = string.Empty;
    public string? MoTa { get; set; }
    public string? TinhTrang { get; set; }
}

/// <summary>Metadata hình ảnh trả về từ sp_LayHinhAnhTheoHoSo.</summary>
public class BodyImageItemDto
{
    public long MaHinhAnh { get; set; }
    public string TenGoc { get; set; } = string.Empty;
    public string DuongDan { get; set; } = string.Empty;
    public string LoaiHinhAnh { get; set; } = string.Empty;
    public string DinhDang { get; set; } = string.Empty;
    public long KichThuoc { get; set; }
    public bool LaNhayCam { get; set; }
    public string? MoTa { get; set; }
    public DateTime ThoiGianTaiLen { get; set; }
    public string NguoiTaiLen { get; set; } = string.Empty;

    public string KichThuocHienThi => KichThuoc >= 1024L * 1024L
        ? $"{KichThuoc / (1024.0 * 1024.0):F2} MB"
        : $"{KichThuoc / 1024.0:F1} KB";

    public string NhanNhayCam => LaNhayCam ? "Nhạy cảm" : "Công khai";
}

/// <summary>Kết quả đối sánh của hồ sơ thi thể từ view v_DoiSanhChiTiet.</summary>
public class BodyMatchResultDto
{
    public long MaDoiSanh { get; set; }
    public long MaHoSoThiThe { get; set; }
    public long MaHoSoMatTich { get; set; }
    public string HoTenNguoiMatTich { get; set; } = string.Empty;
    public decimal? DiemDoiSanh { get; set; }
    public string? MucDoPhuHop { get; set; }
    public string TrangThai { get; set; } = string.Empty;
    public string? TieuChi { get; set; }
    public DateTime ThoiGianYeuCau { get; set; }
    public string NguoiYeuCau { get; set; } = string.Empty;

    public string DiemHienThi => DiemDoiSanh.HasValue ? $"{DiemDoiSanh.Value:0.##}%" : "-";

    public string NhanMucDoPhuHop => MucDoPhuHop switch
    {
        "UuTien" => "Ưu tiên cao",
        "CanXacMinh" => "Cần xác minh",
        "CoKhaNang" => "Có khả năng",
        "Thap" => "Thấp",
        _ => MucDoPhuHop ?? "-"
    };

    public string NhanTrangThai => TrangThai switch
    {
        "ChoXuLy" => "Chờ xử lý",
        "ChuyenXacMinh" => "Đã chuyển xác minh",
        "LoaiBo" => "Đã loại bỏ",
        _ => TrangThai
    };
}
