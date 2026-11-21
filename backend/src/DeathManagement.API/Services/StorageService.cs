using DeathManagement.API.DTOs.Storage;
using DeathManagement.API.DTOs.User;
using DeathManagement.API.Entities;
using DeathManagement.API.Repositories;
using DeathManagement.API.Repositories.Interfaces;
using DeathManagement.API.Security;
using DeathManagement.API.Services.Interfaces;
using static DeathManagement.API.Security.AuthorizationPolicies;

namespace DeathManagement.API.Services;

/// <summary>
/// Dịch vụ nghiệp vụ quản lý danh mục vị trí lưu trữ (Kho lạnh / Nhà xác)
/// </summary>
public class StorageService : IStorageService
{
    private readonly IStorageRepository _storageRepository;

    public StorageService(IStorageRepository? storageRepository = null)
    {
        _storageRepository = storageRepository ?? new StorageRepository();
    }

    public async Task<PagedResult<StorageLocationDto>> GetPagedListAsync(StorageLocationFilterRequest filter)
    {
        AccessControl.Require(ResourceStorage, ActionView);

        var paged = await _storageRepository.GetPagedListAsync(filter);
        return new PagedResult<StorageLocationDto>
        {
            Items = paged.Items.Select(MapToDto).ToList(),
            TotalCount = paged.TotalCount,
            PageIndex = paged.PageIndex,
            PageSize = paged.PageSize
        };
    }

    public async Task<List<StorageLocationDto>> GetAllAsync()
    {
        AccessControl.Require(ResourceStorage, ActionView);

        var items = await _storageRepository.GetAllAsync();
        return items.Select(MapToDto).ToList();
    }

    public async Task<StorageLocationDto?> GetByIdAsync(int id)
    {
        AccessControl.Require(ResourceStorage, ActionView);

        var item = await _storageRepository.GetByIdAsync(id);
        return item == null ? null : MapToDto(item);
    }

    public async Task<StorageLocationDto> CreateAsync(CreateStorageLocationRequest request)
    {
        AccessControl.Require(ResourceStorage, ActionCreate);

        var khu = request.Khu.Trim();
        var tu = request.Tu.Trim();
        var ngan = request.Ngan.Trim();

        if (string.IsNullOrWhiteSpace(khu) || string.IsNullOrWhiteSpace(tu) || string.IsNullOrWhiteSpace(ngan))
        {
            throw new InvalidOperationException("Khu, Tủ và Ngăn lưu trữ không được để trống.");
        }

        if (request.SucChua <= 0)
        {
            throw new InvalidOperationException("Sức chứa của ngăn lưu trữ phải lớn hơn 0.");
        }

        // Kiểm tra ràng buộc duy nhất UQ_ViTriLuuTru_Khu_Tu_Ngan
        if (await _storageRepository.ExistsAsync(khu, tu, ngan))
        {
            throw new InvalidOperationException($"Vị trí lưu trữ [{khu} - {tu} - {ngan}] đã tồn tại trong hệ thống.");
        }

        var entity = new StorageLocation
        {
            Khu = khu,
            Tu = tu,
            Ngan = ngan,
            SucChua = request.SucChua
        };

        var created = await _storageRepository.AddAsync(entity);
        return MapToDto(created);
    }

    public async Task<StorageLocationDto> UpdateAsync(int id, UpdateStorageLocationRequest request)
    {
        AccessControl.Require(ResourceStorage, ActionUpdate);

        var existing = await _storageRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Không tìm thấy vị trí lưu trữ có mã {id}.");

        var khu = request.Khu.Trim();
        var tu = request.Tu.Trim();
        var ngan = request.Ngan.Trim();

        if (string.IsNullOrWhiteSpace(khu) || string.IsNullOrWhiteSpace(tu) || string.IsNullOrWhiteSpace(ngan))
        {
            throw new InvalidOperationException("Khu, Tủ và Ngăn lưu trữ không được để trống.");
        }

        if (request.SucChua <= 0)
        {
            throw new InvalidOperationException("Sức chứa của ngăn lưu trữ phải lớn hơn 0.");
        }

        // Kiểm tra không được trùng với vị trí khác
        if (await _storageRepository.ExistsAsync(khu, tu, ngan, excludeId: id))
        {
            throw new InvalidOperationException($"Vị trí lưu trữ [{khu} - {tu} - {ngan}] đã tồn tại ở mã vị trí khác.");
        }

        existing.Khu = khu;
        existing.Tu = tu;
        existing.Ngan = ngan;
        existing.SucChua = request.SucChua;

        await _storageRepository.UpdateAsync(existing);
        return MapToDto(existing);
    }

    public async Task DeleteAsync(int id)
    {
        // Cho phép xóa nếu có quyền DELETE hoặc quyền UPDATE trên tài nguyên STORAGE
        if (!AccessControl.Can(ResourceStorage, ActionDelete) && !AccessControl.Can(ResourceStorage, ActionUpdate))
        {
            AccessControl.Require(ResourceStorage, ActionDelete);
        }

        var existing = await _storageRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Không tìm thấy vị trí lưu trữ có mã {id}.");

        // Kiểm tra ràng buộc toàn vẹn lịch sử lưu trữ (chống vi phạm khóa ngoại)
        if (await _storageRepository.HasStorageHistoryAsync(id))
        {
            throw new InvalidOperationException($"Không thể xóa vị trí [{existing.Khu} - {existing.Tu} - {existing.Ngan}] vì đã có lịch sử lưu trữ thi thể liên kết.");
        }

        await _storageRepository.DeleteAsync(existing);
    }

    public async Task<List<string>> GetDistinctKhuListAsync()
    {
        AccessControl.Require(ResourceStorage, ActionView);
        return await _storageRepository.GetDistinctKhuListAsync();
    }

    public async Task<List<string>> GetDistinctTuListAsync(string? khu = null)
    {
        AccessControl.Require(ResourceStorage, ActionView);
        return await _storageRepository.GetDistinctTuListAsync(khu);
    }

    private static StorageLocationDto MapToDto(StorageLocation x) => new()
    {
        MaViTri = x.MaViTri,
        Khu = x.Khu,
        Tu = x.Tu,
        Ngan = x.Ngan,
        SucChua = x.SucChua
    };
}
