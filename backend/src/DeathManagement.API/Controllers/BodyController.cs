using DeathManagement.API.DTOs.Body;
using DeathManagement.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DeathManagement.API.Controllers;

[ApiController]
[Route("api/bodies")]
public class BodyController(IBodyService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] BodyFilterRequest filter)
    {
        var result = await service.LayDanhSachAsync(filter);
        return Ok(new { result.Items, result.Total, filter.Trang, filter.SoLuong });
    }
}
