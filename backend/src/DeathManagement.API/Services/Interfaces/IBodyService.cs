using DeathManagement.API.DTOs.Body;

namespace DeathManagement.API.Services.Interfaces;

/// <summary>
/// Giao diện service nghiệp vụ quản lý hồ sơ thi thể và luồng trạng thái (Task 6.1 & State Machine).
/// </summary>
public interface IBodyService
{
    /// <summary>Tạo hồ sơ thi thể mới. Yêu cầu quyền BODY:CREATE.</summary>
    Task<long> TaoHoSoAsync(CreateBodyRequest request);

    /// <summary>Lấy chi tiết cơ bản hồ sơ theo mã. Yêu cầu quyền BODY:VIEW.</summary>
    Task<BodyDto?> LayChiTietAsync(long maHoSoThiThe);

    /// <summary>
    /// Lấy đầy đủ chi tiết hồ sơ thi thể (Task 6.3): Thông tin cơ bản + Tin báo gốc + Vị trí lưu trữ +
    /// Đồ vật cá nhân + Hình ảnh (sp_LayHinhAnhTheoHoSo) + Kết quả đối sánh + Lịch sử trạng thái.
    /// Yêu cầu quyền BODY:VIEW.
    /// </summary>
    Task<BodyDetailDto?> GetByIdAsync(long maHoSoThiThe);

    /// <summary>Lấy danh sách hồ sơ có phân trang. Yêu cầu quyền BODY:VIEW.</summary>
    Task<(List<BodyDto> Items, int Total)> LayDanhSachAsync(string? tuKhoa, string? trangThai, int trang = 1, int soLuong = 20);
    Task<(List<BodyDto> Items, int Total)> LayDanhSachAsync(BodyFilterRequest filter);

    /// <summary>Cập nhật thông tin hồ sơ (và chuyển trạng thái nếu có). Yêu cầu quyền BODY:UPDATE.</summary>
    Task CapNhatAsync(long maHoSoThiThe, UpdateBodyRequest request);

    /// <summary>Thay đổi trạng thái hồ sơ theo quy tắc state machine, kiểm tra quyền và ghi lịch sử. Yêu cầu quyền BODY:UPDATE.</summary>
    Task<BodyDto> ChuyenTrangThaiAsync(long maHoSoThiThe, string trangThaiMoi, string? lyDo = null);

    /// <summary>Lấy lịch sử thay đổi trạng thái của một hồ sơ thi thể. Yêu cầu quyền BODY:VIEW.</summary>
    Task<List<BodyStatusHistoryDto>> LayLichSuTrangThaiAsync(long maHoSoThiThe);

    /// <summary>Lấy danh sách đặc điểm nhận dạng của hồ sơ thi thể (Task 6.7). Yêu cầu quyền BODY:VIEW (ALL).</summary>
    Task<List<BodyCharacteristicDto>> LayDacDiemNhanDangAsync(long maHoSoThiThe);

    /// <summary>Thêm mới một đặc điểm nhận dạng gắn với hồ sơ thi thể (Task 6.7). Yêu cầu quyền BODY:UPDATE (ALL).</summary>
    Task<BodyCharacteristicDto> ThemDacDiemNhanDangAsync(long maHoSoThiThe, SaveBodyCharacteristicRequest request);

    /// <summary>Cập nhật một đặc điểm nhận dạng đã có của hồ sơ thi thể (Task 6.7). Yêu cầu quyền BODY:UPDATE (ALL).</summary>
    Task<BodyCharacteristicDto> CapNhatDacDiemNhanDangAsync(long maHoSoThiThe, long maDacDiem, SaveBodyCharacteristicRequest request);
}
