namespace DeathManagement.API.DTOs.Image;

public record ImageDto(
    long MaHinhAnh,
    string TenFile,
    string TenGoc,
    string DuongDan,
    string LoaiHinhAnh,
    string DinhDang,
    long KichThuoc,
    int? ChieuRong,
    int? ChieuCao,
    string? HashMD5,
    bool LaNhayCam,
    string? MoTa,
    long? MaHoSoThiThe,
    long? MaHoSoMatTich,
    int? MaTinBao,
    long? MaXacMinh,
    long MaTaiKhoanTaiLen,
    string NguoiTaiLen,
    DateTime ThoiGianTaiLen,
    string TrangThai
);
