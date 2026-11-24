namespace DeathManagement.API.DTOs.Body;

/// <summary>
/// DTO cập nhật thông tin hồ sơ thi thể và/hoặc thay đổi trạng thái hồ sơ (BODY:UPDATE).
/// </summary>
public class UpdateBodyRequest
{
    /// <summary>Thời gian phát hiện (không được ở tương lai).</summary>
    public DateTime? ThoiGianPhatHien { get; set; }

    /// <summary>Địa điểm phát hiện.</summary>
    public string? DiaDiem { get; set; }

    /// <summary>Nam | Nu | Khac | KhongXacDinh.</summary>
    public string? GioiTinh { get; set; }

    /// <summary>Tuổi ước lượng (0–130).</summary>
    public short? TuoiUocLuong { get; set; }

    /// <summary>Chiều cao (cm).</summary>
    public decimal? ChieuCao { get; set; }

    /// <summary>Đặc điểm cơ thể.</summary>
    public string? DacDiemCoThe { get; set; }

    /// <summary>
    /// Trạng thái mới (nếu có yêu cầu chuyển trạng thái):
    /// MoiTiepNhan | DangLuuGiu | DangXacMinh | DaXacDinh | DaBanGiao | DongHoSo | TiepTucLuuGiu.
    /// </summary>
    public string? TrangThai { get; set; }

    /// <summary>Lý do hoặc ghi chú khi thay đổi trạng thái (bắt buộc khi mở lại hồ sơ đã đóng).</summary>
    public string? LyDoChuyenTrangThai { get; set; }
}

/// <summary>
/// DTO chuyên biệt dùng cho API thay đổi trạng thái hồ sơ thi thể.
/// </summary>
public class ChangeBodyStatusRequest
{
    /// <summary>Trạng thái đích muốn chuyển tới.</summary>
    public string TrangThaiMoi { get; set; } = string.Empty;

    /// <summary>Lý do / ghi chú chuyển trạng thái (bắt buộc khi mở lại từ DongHoSo -> TiepTucLuuGiu).</summary>
    public string? LyDo { get; set; }
}

/// <summary>
/// DTO trả về lịch sử thay đổi trạng thái và thao tác trên hồ sơ thi thể.
/// </summary>
public class BodyStatusHistoryDto
{
    public long MaNhatKy { get; set; }
    public long MaHoSoThiThe { get; set; }
    public string HanhDong { get; set; } = string.Empty;
    public string? TrangThaiCu { get; set; }
    public string? TrangThaiMoi { get; set; }
    public string NhanTrangThaiCu => string.IsNullOrEmpty(TrangThaiCu) ? "-" : Entities.Body.GetStatusLabel(TrangThaiCu);
    public string NhanTrangThaiMoi => string.IsNullOrEmpty(TrangThaiMoi) ? "-" : Entities.Body.GetStatusLabel(TrangThaiMoi);
    public string? LyDo { get; set; }
    public string? ChiTiet { get; set; }
    public DateTime ThoiGian { get; set; }
    public long? MaTaiKhoan { get; set; }
    public string? NguoiThucHien { get; set; }
}
