using DeathManagement.API.DTOs.Body;
using DeathManagement.API.Entities;

namespace DeathManagement.API.Services.Interfaces;

/// <summary>
/// Giao diện service nghiệp vụ quản lý hồ sơ thi thể (Task 6.1).
/// </summary>
public interface IBodyService
{
    /// <summary>Tạo hồ sơ thi thể mới. Yêu cầu quyền BODY:CREATE.</summary>
    Task<long> TaoHoSoAsync(CreateBodyRequest request);

    /// <summary>Lấy chi tiết hồ sơ theo mã. Yêu cầu quyền BODY:VIEW.</summary>
    Task<BodyDto?> LayChiTietAsync(long maHoSoThiThe);

    /// <summary>Lấy danh sách hồ sơ có phân trang. Yêu cầu quyền BODY:VIEW.</summary>
    Task<(List<BodyDto> Items, int Total)> LayDanhSachAsync(string? tuKhoa, string? trangThai, int trang = 1, int soLuong = 20);

    /// <summary>Cập nhật thông tin hồ sơ. Yêu cầu quyền BODY:UPDATE.</summary>
    Task CapNhatAsync(long maHoSoThiThe, UpdateBodyRequest request);
}
