using DeathManagement.API.DTOs.Storage;
using DeathManagement.API.DTOs.User;
using DeathManagement.API.Entities;

namespace DeathManagement.API.Repositories.Interfaces;

public interface IStorageRepository
{
    Task<PagedResult<StorageLocation>> GetPagedListAsync(StorageLocationFilterRequest filter);
    Task<List<StorageLocation>> GetAllAsync();
    Task<StorageLocation?> GetByIdAsync(int id);
    Task<bool> ExistsAsync(string khu, string tu, string ngan, int? excludeId = null);
    Task<bool> HasStorageHistoryAsync(int id);
    Task<StorageLocation> AddAsync(StorageLocation location);
    Task UpdateAsync(StorageLocation location);
    Task DeleteAsync(StorageLocation location);
    Task<List<string>> GetDistinctKhuListAsync();
    Task<List<string>> GetDistinctTuListAsync(string? khu = null);

    /// <summary>
    /// Thống kê danh sách các Khu lưu trữ kèm sức chứa, tình trạng lấp đầy và cảnh báo (Task 7.2)
    /// </summary>
    Task<List<StorageZoneDto>> GetZonesSummaryAsync();

    /// <summary>
    /// Lấy danh sách các Tủ trong một Khu kèm thông tin sức chứa và danh sách ngăn (Task 7.2)
    /// </summary>
    Task<List<StorageCabinetDto>> GetCabinetsByZoneAsync(string khu);

    /// <summary>
    /// Thống kê chi tiết một Khu lưu trữ cụ thể (Task 7.2)
    /// </summary>
    Task<StorageZoneDto?> GetZoneSummaryAsync(string khu);
}
