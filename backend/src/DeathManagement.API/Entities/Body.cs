namespace DeathManagement.API.Entities;

/// <summary>
/// Ánh xạ bảng HoSoThiThe — hồ sơ thi thể chưa xác định danh tính.
/// Quản lý tập trung danh sách trạng thái chính thức và quy tắc chuyển đổi trạng thái (State Machine).
/// </summary>
public class Body
{
    // ─────────────── Danh mục trạng thái chính thức ───────────────
    public const string StatusMoiTiepNhan   = "MoiTiepNhan";
    public const string StatusDangLuuGiu    = "DangLuuGiu";
    public const string StatusDangXacMinh   = "DangXacMinh";
    public const string StatusDaXacDinh     = "DaXacDinh";
    public const string StatusDaBanGiao     = "DaBanGiao";
    public const string StatusDongHoSo      = "DongHoSo";
    public const string StatusTiepTucLuuGiu = "TiepTucLuuGiu";

    /// <summary>Tất cả trạng thái hợp lệ của hồ sơ thi thể (khớp CK_HoSoThiThe_TrangThai).</summary>
    public static readonly IReadOnlyList<string> ValidStatuses =
    [
        StatusMoiTiepNhan,
        StatusDangLuuGiu,
        StatusDangXacMinh,
        StatusDaXacDinh,
        StatusDaBanGiao,
        StatusDongHoSo,
        StatusTiepTucLuuGiu
    ];

    /// <summary>
    /// Ma trận chuyển đổi trạng thái hợp lệ (khớp 100% với fn_ChuyenTrangThaiHopLe('HO_SO_THI_THE', ...)).
    /// </summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> AllowedTransitions =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            [StatusMoiTiepNhan]   = [StatusDangLuuGiu, StatusDangXacMinh],
            [StatusDangLuuGiu]    = [StatusDangXacMinh],
            [StatusDangXacMinh]   = [StatusDaXacDinh, StatusTiepTucLuuGiu],
            [StatusTiepTucLuuGiu] = [StatusDangXacMinh],
            [StatusDaXacDinh]     = [StatusDaBanGiao],
            [StatusDaBanGiao]     = [StatusDongHoSo],
            [StatusDongHoSo]      = [StatusTiepTucLuuGiu]
        };

    // ─────────────── Thuộc tính cột bảng HoSoThiThe ───────────────
    public long MaHoSoThiThe { get; set; }

    /// <summary>Thời điểm phát hiện thi thể.</summary>
    public DateTime ThoiGianPhatHien { get; set; }

    /// <summary>Địa điểm phát hiện.</summary>
    public string? DiaDiem { get; set; }

    /// <summary>Trạng thái hồ sơ theo state machine: MoiTiepNhan, DangLuuGiu, DangXacMinh, DaXacDinh, DaBanGiao, DongHoSo, TiepTucLuuGiu.</summary>
    public string TrangThai { get; set; } = StatusMoiTiepNhan;

    /// <summary>Giới tính ước tính: Nam, Nu, Khac, KhongXacDinh.</summary>
    public string? GioiTinh { get; set; }

    /// <summary>Tuổi ước lượng (0–130).</summary>
    public short? TuoiUocLuong { get; set; }

    /// <summary>Chiều cao đơn vị cm.</summary>
    public decimal? ChieuCao { get; set; }

    /// <summary>Mô tả đặc điểm cơ thể (vết sẹo, hình xăm, v.v.).</summary>
    public string? DacDiemCoThe { get; set; }

    /// <summary>Vector đặc trưng khuôn mặt (AI face embedding).</summary>
    public byte[]? KhuonMat { get; set; }

    /// <summary>Liên kết Tin báo tương ứng (nếu có).</summary>
    public int? MaTinBao { get; set; }

    /// <summary>Danh sách đặc điểm nhận dạng có cấu trúc gắn với hồ sơ thi thể (Task 6.7).</summary>
    public ICollection<BodyCharacteristic> DacDiemNhanDangs { get; set; } = new List<BodyCharacteristic>();

    // ─────────────── Hàm tiện ích trạng thái ───────────────

    public static bool IsValidStatus(string? status)
        => !string.IsNullOrWhiteSpace(status) && ValidStatuses.Contains(status.Trim(), StringComparer.Ordinal);

    public static bool CanTransition(string? fromStatus, string? toStatus)
    {
        if (string.IsNullOrWhiteSpace(fromStatus) || string.IsNullOrWhiteSpace(toStatus))
            return false;
        return AllowedTransitions.TryGetValue(fromStatus.Trim(), out var nexts)
            && nexts.Contains(toStatus.Trim(), StringComparer.Ordinal);
    }

    public static IReadOnlyList<string> GetAllowedNextStatuses(string? currentStatus)
        => !string.IsNullOrWhiteSpace(currentStatus) && AllowedTransitions.TryGetValue(currentStatus.Trim(), out var nexts)
            ? nexts
            : [];

    public static string GetStatusLabel(string? status) => status switch
    {
        StatusMoiTiepNhan   => "Mới tiếp nhận",
        StatusDangLuuGiu    => "Đang lưu giữ",
        StatusDangXacMinh   => "Đang xác minh",
        StatusDaXacDinh     => "Đã xác định",
        StatusDaBanGiao     => "Đã bàn giao",
        StatusDongHoSo      => "Đóng hồ sơ",
        StatusTiepTucLuuGiu => "Tiếp tục lưu giữ",
        _                   => status ?? "Không xác định"
    };

    public static string GetStatusColorHex(string? status) => status switch
    {
        StatusMoiTiepNhan   => "#D6EAF8", // Xanh dương nhạt
        StatusDangLuuGiu    => "#FDEBD0", // Cam nhạt
        StatusDangXacMinh   => "#FCF3CF", // Vàng nhạt
        StatusDaXacDinh     => "#D5F5E3", // Xanh lá nhạt
        StatusDaBanGiao     => "#E8DAEF", // Tím nhạt
        StatusDongHoSo      => "#E5E7E9", // Xám nhạt
        StatusTiepTucLuuGiu => "#FADBD8", // Đỏ hồng nhạt
        _                   => "#F2F4F4"
    };

    public static string GetStatusTextColorHex(string? status) => status switch
    {
        StatusMoiTiepNhan   => "#1B4F72",
        StatusDangLuuGiu    => "#784212",
        StatusDangXacMinh   => "#7D6608",
        StatusDaXacDinh     => "#145A32",
        StatusDaBanGiao     => "#4A235A",
        StatusDongHoSo      => "#424949",
        StatusTiepTucLuuGiu => "#78281F",
        _                   => "#1E293B"
    };
}

/// <summary>
/// Ánh xạ bảng DacDiemNhanDang — quản lý từng đặc điểm nhận dạng nổi bật gắn với hồ sơ thi thể (Task 6.7).
/// </summary>
public class BodyCharacteristic
{
    public const string TypeSeo       = "Seo";
    public const string TypeHinhXam   = "HinhXam";
    public const string TypeVetBot    = "VetBot";
    public const string TypeRang      = "Rang";
    public const string TypeDiTat     = "DiTat";
    public const string TypeTocDa     = "TocDa";
    public const string TypeTrangPhuc = "TrangPhuc";
    public const string TypeKhac      = "Khac";

    public static readonly IReadOnlyList<string> ValidTypes =
    [
        TypeSeo,
        TypeHinhXam,
        TypeVetBot,
        TypeRang,
        TypeDiTat,
        TypeTocDa,
        TypeTrangPhuc,
        TypeKhac
    ];

    public long MaDacDiem { get; set; }
    public long MaHoSoThiThe { get; set; }
    public string LoaiDacDiem { get; set; } = TypeKhac;
    public string? ViTriCoThe { get; set; }
    public string MoTa { get; set; } = string.Empty;
    public DateTime NgayGhiNhan { get; set; } = DateTime.Now;

    public Body? HoSoThiThe { get; set; }

    public static bool IsValidType(string? type)
        => !string.IsNullOrWhiteSpace(type) && ValidTypes.Contains(type.Trim(), StringComparer.Ordinal);

    public static string GetTypeLabel(string? type) => type switch
    {
        TypeSeo       => "Vết sẹo",
        TypeHinhXam   => "Hình xăm",
        TypeVetBot    => "Vết bớt",
        TypeRang      => "Đặc điểm răng",
        TypeDiTat     => "Dị tật",
        TypeTocDa     => "Tóc / Da",
        TypeTrangPhuc => "Trang phục",
        TypeKhac      => "Đặc điểm khác",
        _             => type ?? "Đặc điểm khác"
    };
}
