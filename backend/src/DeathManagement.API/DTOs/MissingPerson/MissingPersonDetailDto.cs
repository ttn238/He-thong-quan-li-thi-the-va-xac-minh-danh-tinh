using MissingEntity = DeathManagement.API.Entities.MissingPerson;

namespace DeathManagement.API.DTOs.MissingPerson;

/// <summary>
/// DTO tổng hợp đầy đủ chi tiết hồ sơ người mất tích (Task 8.4 — Xem chi tiết).
/// Bao gồm: Thông tin hành chính & nhân trắc học, danh sách người thân, album ảnh (sp_LayHinhAnhTheoHoSo)
/// và lịch sử các ca đối sánh nghi vấn với thi thể (v_DoiSanhChiTiet).
/// </summary>
public class MissingPersonDetailDto
{
    public long MaHoSoMatTich { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public DateTime? NgaySinh { get; set; }
    public string? GioiTinh { get; set; }
    public DateTime? ThoiGianMatTich { get; set; }
    public string? NoiMatTich { get; set; }
    public string TrangThai { get; set; } = MissingEntity.StatusDangTimKiem;

    public int? TuoiUocTinh => NgaySinh.HasValue
        ? Math.Max(0, DateTime.Today.Year - NgaySinh.Value.Year - (DateTime.Today < NgaySinh.Value.AddYears(DateTime.Today.Year - NgaySinh.Value.Year) ? 1 : 0))
        : null;

    public string NhanGioiTinh => MissingEntity.GetGenderLabel(GioiTinh);
    public string NhanTrangThai => MissingEntity.GetStatusLabel(TrangThai);
    public string MauTrangThai => MissingEntity.GetStatusColorHex(TrangThai);
    public string MauChuTrangThai => MissingEntity.GetStatusTextColorHex(TrangThai);

    /// <summary>Danh sách tất cả người thân liên hệ của hồ sơ (bảng NguoiThan).</summary>
    public List<MissingPersonRelativeDto> NguoiThan { get; set; } = new();

    /// <summary>Danh sách hình ảnh đã upload (lấy qua sp_LayHinhAnhTheoHoSo với @MaHoSoMatTich = id).</summary>
    public List<MissingPersonImageDto> HinhAnh { get; set; } = new();

    /// <summary>Danh sách các ca đối sánh nghi vấn đã phát sinh với thi thể (từ view v_DoiSanhChiTiet).</summary>
    public List<MissingPersonMatchDto> KetQuaDoiSanh { get; set; } = new();

    public bool CoQuyenXemHinhAnh { get; set; }
    public bool CoQuyenXemDoiSanh { get; set; }
}

/// <summary>Thông tin một người thân liên hệ (bảng NguoiThan).</summary>
public class MissingPersonRelativeDto
{
    public long MaNguoiThan { get; set; }
    public long MaHoSoMatTich { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public string? QuanHe { get; set; }
    public string? SoDienThoai { get; set; }
    public string? DiaChi { get; set; }
}

/// <summary>Thông tin hình ảnh trả về từ sp_LayHinhAnhTheoHoSo.</summary>
public class MissingPersonImageDto
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
    public string NguoiTaiLen { get; set; } = "Hệ thống";

    public string KichThuocHienThi => KichThuoc switch
    {
        >= 1024 * 1024 => $"{KichThuoc / (1024.0 * 1024.0):0.##} MB",
        >= 1024        => $"{KichThuoc / 1024.0:0.#} KB",
        _              => $"{KichThuoc} B"
    };
}

/// <summary>Thông tin một ca đối sánh nghi vấn với thi thể (view v_DoiSanhChiTiet).</summary>
public class MissingPersonMatchDto
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
    public string? NguoiYeuCau { get; set; }

    public string DiemHienThi => DiemDoiSanh.HasValue ? $"{DiemDoiSanh.Value:0.##}%" : "—";

    public string NhanMucDoPhuHop => MucDoPhuHop switch
    {
        "RatCao"      => "Rất cao",
        "Cao"         => "Cao",
        "CanXacMinh"  => "Cần xác minh",
        "TrungBinh"   => "Trung bình",
        "Thap"        => "Thấp",
        _             => MucDoPhuHop ?? "—"
    };

    public string NhanTrangThai => TrangThai switch
    {
        "ChoXuLy"      => "Chờ xử lý",
        "DangXacMinh"  => "Đang xác minh",
        "DaXacNhan"    => "Đã xác nhận",
        "LoaiBo"       => "Loại bỏ",
        _              => TrangThai
    };
}
