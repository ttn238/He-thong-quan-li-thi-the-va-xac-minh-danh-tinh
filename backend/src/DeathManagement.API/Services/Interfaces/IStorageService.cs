using DeathManagement.API.DTOs.Storage;
using DeathManagement.API.DTOs.User;

namespace DeathManagement.API.Services.Interfaces;

public interface IStorageService
{
    Task<PagedResult<StorageLocationDto>> GetPagedListAsync(StorageLocationFilterRequest filter);
    Task<List<StorageLocationDto>> GetAllAsync();
    Task<StorageLocationDto?> GetByIdAsync(int id);
    Task<StorageLocationDto> CreateAsync(CreateStorageLocationRequest request);
    Task<StorageLocationDto> UpdateAsync(int id, UpdateStorageLocationRequest request);
    Task DeleteAsync(int id);
    Task<List<string>> GetDistinctKhuListAsync();
    Task<List<string>> GetDistinctTuListAsync(string? khu = null);

    /// <summary>
    /// Thống kê danh sách Khu lưu trữ kèm sức chứa và cảnh báo 100% (Task 7.2)
    /// </summary>
    Task<List<StorageZoneDto>> GetZonesAsync();

    /// <summary>
    /// Thống kê danh sách Tủ lưu trữ trong một Khu (Task 7.2)
    /// </summary>
    Task<List<StorageCabinetDto>> GetCabinetsByZoneAsync(string khu);

    /// <summary>
    /// Lấy chi tiết thống kê một Khu lưu trữ (Task 7.2)
    /// </summary>
    Task<StorageZoneDto?> GetZoneAsync(string khu);

    /// <summary>
    /// Lấy dòng thời gian lịch sử di chuyển vị trí lưu trữ của hồ sơ thi thể (Task 7.7)
    /// </summary>
    Task<List<StorageMovementHistoryDto>> GetMovementHistoryAsync(long maHoSoThiThe);
}
