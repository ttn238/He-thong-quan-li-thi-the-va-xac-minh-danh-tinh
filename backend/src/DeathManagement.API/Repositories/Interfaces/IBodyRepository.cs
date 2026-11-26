using DeathManagement.API.DTOs.Body;
using DeathManagement.API.Entities;

namespace DeathManagement.API.Repositories.Interfaces;

/// <summary>
/// Giao diện repository thao tác dữ liệu hồ sơ thi thể.
/// </summary>
public interface IBodyRepository
{
    /// <summary>Tạo hồ sơ thi thể mới bằng sp_LapHoSoThiThe, trả về mã hồ sơ.</summary>
    Task<long> TaoHoSoAsync(CreateBodyRequest request, long maTaiKhoan);

    /// <summary>Lấy hồ sơ theo mã.</summary>
    Task<Body?> LayTheoMaAsync(long maHoSoThiThe);

    /// <summary>Lấy chi tiết cơ bản (kèm thông tin vị trí lưu trữ hiện tại).</summary>
    Task<BodyDto?> LayChiTietDtoAsync(long maHoSoThiThe, bool nhaXac);

    /// <summary>
    /// Tổng hợp đầy đủ chi tiết hồ sơ thi thể (Task 6.3):
    /// HoSoThiThe + TinBao + v_ViTriHienTai/ViTriLuuTru/LichSuLuuTru + DoVatCaNhan + sp_LayHinhAnhTheoHoSo + v_DoiSanhChiTiet + NhatKyHeThong.
    /// </summary>
    Task<BodyDetailDto?> LayChiTietDayDuAsync(
        long maHoSoThiThe,
        long maTaiKhoan,
        bool nhaXac,
        bool coQuyenXemDoVat,
        bool coQuyenXemHinhAnh,
        bool coQuyenXemDoiSanh);

    /// <summary>Lấy danh sách có phân trang, lọc theo từ khóa và trạng thái.</summary>
    Task<(List<BodyDto> Items, int Total)> LayDanhSachAsync(string? tuKhoa, string? trangThai, int trang, int soLuong);
    Task<(List<BodyDto> Items, int Total)> LayDanhSachAsync(BodyFilterRequest filter, bool nhaXac);

    /// <summary>Cập nhật thông tin hồ sơ thi thể.</summary>
    Task CapNhatAsync(long maHoSoThiThe, UpdateBodyRequest request, long maTaiKhoan);

    /// <summary>Thay đổi trạng thái hồ sơ thi thể theo state machine và ghi lịch sử truy vết.</summary>
    Task ChuyenTrangThaiAsync(long maHoSoThiThe, string trangThaiMoi, string? lyDo, long maTaiKhoan);

    /// <summary>Lấy lịch sử thay đổi trạng thái của hồ sơ thi thể từ bảng NhatKyHeThong.</summary>
    Task<List<BodyStatusHistoryDto>> LayLichSuTrangThaiAsync(long maHoSoThiThe);

    /// <summary>Lấy danh sách đặc điểm nhận dạng gắn với hồ sơ thi thể (Task 6.7).</summary>
    Task<List<BodyCharacteristicDto>> LayDacDiemNhanDangAsync(long maHoSoThiThe)
        => Task.FromResult(new List<BodyCharacteristicDto>());

    /// <summary>Thêm mới một đặc điểm nhận dạng cho hồ sơ thi thể (Task 6.7).</summary>
    Task<BodyCharacteristicDto> ThemDacDiemNhanDangAsync(long maHoSoThiThe, SaveBodyCharacteristicRequest request, long maTaiKhoan)
        => throw new NotSupportedException();

    /// <summary>Cập nhật một đặc điểm nhận dạng đã có của hồ sơ thi thể (Task 6.7).</summary>
    Task<BodyCharacteristicDto> CapNhatDacDiemNhanDangAsync(long maHoSoThiThe, long maDacDiem, SaveBodyCharacteristicRequest request, long maTaiKhoan)
        => throw new NotSupportedException();
}
