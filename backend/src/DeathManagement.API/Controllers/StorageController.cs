using DeathManagement.API.DTOs.Storage;
using DeathManagement.API.DTOs.User;
using DeathManagement.API.Services;
using DeathManagement.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeathManagement.API.Controllers;

[ApiController]
[Route("api/v1/storage-locations")]
public class StorageController : ControllerBase
{
    private readonly IStorageService _storageService;

    public StorageController(IStorageService? storageService = null)
    {
        _storageService = storageService ?? new StorageService();
    }

    /// <summary>
    /// Lấy danh sách vị trí lưu trữ có phân trang và lọc theo Khu, Tủ, từ khóa
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<PagedResult<StorageLocationDto>>> GetPaged([FromQuery] StorageLocationFilterRequest filter)
    {
        var result = await _storageService.GetPagedListAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Lấy chi tiết thông tin một vị trí lưu trữ theo mã định danh
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<ActionResult<StorageLocationDto>> GetById(int id)
    {
        var location = await _storageService.GetByIdAsync(id);
        if (location == null)
            return NotFound(new { message = $"Không tìm thấy vị trí lưu trữ với mã #{id}." });

        return Ok(location);
    }

    /// <summary>
    /// Lấy danh sách các Khu lưu trữ hiện có để nạp vào bộ lọc dropdown
    /// </summary>
    [HttpGet("areas")]
    [Authorize]
    public async Task<ActionResult<List<string>>> GetAreas()
    {
        var areas = await _storageService.GetDistinctKhuListAsync();
        return Ok(areas);
    }

    /// <summary>
    /// Thêm mới một vị trí ngăn lưu trữ (Yêu cầu quyền STORAGE:CREATE)
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<StorageLocationDto>> Create([FromBody] CreateStorageLocationRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var created = await _storageService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = created.MaViTri }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Cập nhật thông tin vị trí lưu trữ (Yêu cầu quyền STORAGE:UPDATE)
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<ActionResult<StorageLocationDto>> Update(int id, [FromBody] UpdateStorageLocationRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var updated = await _storageService.UpdateAsync(id, request);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Xóa một vị trí lưu trữ (Chỉ khi chưa có lịch sử lưu trữ thi thể)
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            await _storageService.DeleteAsync(id);
            return Ok(new { message = $"Đã xóa thành công vị trí lưu trữ #{id}." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
