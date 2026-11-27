using DeathManagement.API.DTOs.MissingPerson;
using DeathManagement.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DeathManagement.API.Controllers;

/// <summary>
/// API quản lý và tra cứu hồ sơ người mất tích (Task 8.4).
/// </summary>
[ApiController]
[Route("api/v1/missing-persons")]
[Route("api/missing-persons")]
public class MissingPersonController(IMissingPersonService service) : ControllerBase
{
    /// <summary>
    /// Xem chi tiết đầy đủ hồ sơ người mất tích theo mã định danh (GET /api/v1/missing-persons/{id}).
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ActionResult<MissingPersonDetailDto>> GetById(long id)
    {
        if (id <= 0)
            return BadRequest(new { message = "Mã hồ sơ người mất tích không hợp lệ." });

        var detail = await service.GetByIdAsync(id);
        if (detail == null)
            return NotFound(new { message = $"Không tìm thấy hồ sơ người mất tích #{id}." });

        return Ok(detail);
    }
}
