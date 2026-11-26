using DeathManagement.API.DTOs.Body;
using DeathManagement.API.Entities;
using DeathManagement.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DeathManagement.API.Controllers;

/// <summary>
/// API quản lý hồ sơ thi thể và điều phối chuyển đổi trạng thái hồ sơ theo State Machine.
/// Hỗ trợ cả hai đường dẫn gốc: api/bodies và api/v1/bodies.
/// </summary>
[ApiController]
[Route("api/bodies")]
[Route("api/v1/bodies")]
public class BodyController(IBodyService service) : ControllerBase
{
    /// <summary>
    /// Lấy danh sách hồ sơ thi thể có phân trang, tìm kiếm và lọc trạng thái (Yêu cầu quyền BODY:VIEW).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] BodyFilterRequest filter)
    {
        var result = await service.LayDanhSachAsync(filter);
        return Ok(new { result.Items, result.Total, filter.Trang, filter.SoLuong });
    }

    /// <summary>
    /// Lấy danh mục các trạng thái chính thức và quy tắc chuyển trạng thái hợp lệ (State Machine).
    /// </summary>
    [HttpGet("status-rules")]
    public IActionResult GetStatusRules()
    {
        var rules = Body.ValidStatuses.Select(s => new
        {
            TrangThai = s,
            NhanTrangThai = Body.GetStatusLabel(s),
            MauNen = Body.GetStatusColorHex(s),
            MauChu = Body.GetStatusTextColorHex(s),
            ChuyenSangHopLe = Body.GetAllowedNextStatuses(s).Select(next => new
            {
                TrangThai = next,
                NhanTrangThai = Body.GetStatusLabel(next)
            })
        });
        return Ok(rules);
    }

    /// <summary>
    /// Lấy chi tiết đầy đủ một hồ sơ thi thể theo mã định danh (Task 6.3 — Yêu cầu quyền BODY:VIEW).
    /// Bao gồm thông tin cơ bản, tin báo gốc, vị trí lưu trữ, đồ vật cá nhân, hình ảnh, đối sánh và lịch sử trạng thái.
    /// </summary>
    [HttpGet("{id:long}")]
    [HttpGet("{id:long}/detail")]
    public async Task<IActionResult> GetById(long id)
    {
        var body = await service.GetByIdAsync(id);
        if (body == null)
            return NotFound(new { message = $"Không tìm thấy hồ sơ thi thể #{id}." });

        return Ok(body);
    }

    /// <summary>
    /// Lập hồ sơ thi thể mới (Yêu cầu quyền BODY:CREATE).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBodyRequest request)
    {
        var id = await service.TaoHoSoAsync(request);
        var created = await service.LayChiTietAsync(id);
        return CreatedAtAction(nameof(GetById), new { id }, (object?)created ?? new { MaHoSoThiThe = id });
    }

    /// <summary>
    /// Cập nhật thông tin hồ sơ thi thể và/hoặc trạng thái hồ sơ (Task 6.4 — Yêu cầu quyền BODY:UPDATE).
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateBodyRequest? request)
    {
        if (request == null)
            return BadRequest(new { message = "Dữ liệu cập nhật không hợp lệ." });

        await service.CapNhatAsync(id, request);
        var updated = await service.LayChiTietAsync(id);
        return Ok(new
        {
            message = "Cập nhật hồ sơ thi thể thành công.",
            data = updated
        });
    }

    /// <summary>
    /// API thay đổi trạng thái hồ sơ thi thể theo quy tắc State Machine, kiểm tra quyền và chặn chuyển đổi không hợp lệ.
    /// </summary>
    [HttpPut("{id:long}/status")]
    [HttpPatch("{id:long}/status")]
    public async Task<IActionResult> ChangeStatus(long id, [FromBody] ChangeBodyStatusRequest request)
    {
        var updated = await service.ChuyenTrangThaiAsync(id, request.TrangThaiMoi, request.LyDo);
        return Ok(new
        {
            message = $"Đã chuyển trạng thái hồ sơ #{id} sang '{updated.NhanTrangThai}' thành công.",
            data = updated
        });
    }

    /// <summary>
    /// Đóng hồ sơ thi thể sau khi đã bàn giao (DaBanGiao -> DongHoSo).
    /// </summary>
    [HttpPost("{id:long}/close")]
    public async Task<IActionResult> Close(long id, [FromBody] ChangeBodyStatusRequest? request = null)
    {
        var updated = await service.ChuyenTrangThaiAsync(id, Body.StatusDongHoSo, request?.LyDo);
        return Ok(new
        {
            message = $"Đã đóng hồ sơ thi thể #{id}.",
            data = updated
        });
    }

    /// <summary>
    /// Mở lại hồ sơ thi thể đã đóng (DongHoSo -> TiepTucLuuGiu), bắt buộc có lý do.
    /// </summary>
    [HttpPost("{id:long}/reopen")]
    public async Task<IActionResult> Reopen(long id, [FromBody] ChangeBodyStatusRequest request)
    {
        var updated = await service.ChuyenTrangThaiAsync(id, Body.StatusTiepTucLuuGiu, request.LyDo);
        return Ok(new
        {
            message = $"Đã mở lại hồ sơ thi thể #{id} sang trạng thái '{updated.NhanTrangThai}'.",
            data = updated
        });
    }

    /// <summary>
    /// Lấy lịch sử thay đổi trạng thái của hồ sơ thi thể để phục vụ truy vết (Yêu cầu quyền BODY:VIEW).
    /// </summary>
    [HttpGet("{id:long}/status-history")]
    public async Task<IActionResult> GetStatusHistory(long id)
    {
        var history = await service.LayLichSuTrangThaiAsync(id);
        return Ok(history);
    }
}
