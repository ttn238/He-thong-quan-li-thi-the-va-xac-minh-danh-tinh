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

    /// <summary>Lấy chi tiết đầy đủ (kèm thông tin vị trí lưu trữ hiện tại).</summary>
    Task<BodyDto?> LayChiTietDtoAsync(long maHoSoThiThe, bool nhaXac);

    /// <summary>Lấy danh sách có phân trang, lọc theo từ khóa và trạng thái.</summary>
    Task<(List<BodyDto> Items, int Total)> LayDanhSachAsync(string? tuKhoa, string? trangThai, int trang, int soLuong);
    Task<(List<BodyDto> Items, int Total)> LayDanhSachAsync(BodyFilterRequest filter, bool nhaXac);

    /// <summary>Cập nhật thông tin hồ sơ thi thể.</summary>
    Task CapNhatAsync(long maHoSoThiThe, UpdateBodyRequest request, long maTaiKhoan);

    /// <summary>Thay đổi trạng thái hồ sơ thi thể theo state machine và ghi lịch sử truy vết.</summary>
    Task ChuyenTrangThaiAsync(long maHoSoThiThe, string trangThaiMoi, string? lyDo, long maTaiKhoan);

    /// <summary>Lấy lịch sử thay đổi trạng thái của hồ sơ thi thể từ bảng NhatKyHeThong.</summary>
    Task<List<BodyStatusHistoryDto>> LayLichSuTrangThaiAsync(long maHoSoThiThe);
}
