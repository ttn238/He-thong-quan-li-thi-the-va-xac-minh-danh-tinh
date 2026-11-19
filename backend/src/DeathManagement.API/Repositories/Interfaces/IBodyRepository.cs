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

    /// <summary>Lấy danh sách có phân trang, lọc theo từ khóa và trạng thái.</summary>
    Task<(List<BodyDto> Items, int Total)> LayDanhSachAsync(string? tuKhoa, string? trangThai, int trang, int soLuong);

    /// <summary>Cập nhật thông tin hồ sơ thi thể.</summary>
    Task CapNhatAsync(long maHoSoThiThe, UpdateBodyRequest request, long maTaiKhoan);
}
