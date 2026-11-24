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
}
