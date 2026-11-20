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
}
