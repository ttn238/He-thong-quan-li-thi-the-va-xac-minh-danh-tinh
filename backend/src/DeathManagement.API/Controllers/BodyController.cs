using DeathManagement.API.DTOs.Body;
using DeathManagement.API.DTOs.Image;
using DeathManagement.API.Entities;
using DeathManagement.API.Helpers;
using DeathManagement.API.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DeathManagement.API.Controllers;

/// <summary>
/// API quản lý hồ sơ thi thể và điều phối chuyển đổi trạng thái hồ sơ theo State Machine.
/// Hỗ trợ cả hai đường dẫn gốc: api/bodies và api/v1/bodies.
/// </summary>
[ApiController]
[Route("api/bodies")]
[Route("api/v1/bodies")]
public class BodyController(IBodyService service, IImageService? images = null) : ControllerBase
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

    /// <summary>
    /// Lấy danh sách đặc điểm nhận dạng của hồ sơ thi thể (Task 6.7 — Yêu cầu quyền BODY:VIEW đầy đủ).
    /// </summary>
    [HttpGet("{id:long}/characteristics")]
    public async Task<IActionResult> GetCharacteristics(long id)
    {
        var items = await service.LayDacDiemNhanDangAsync(id);
        return Ok(items);
    }

    /// <summary>
    /// Thêm mới một đặc điểm nhận dạng cho hồ sơ thi thể (Task 6.7 — Yêu cầu quyền BODY:UPDATE đầy đủ).
    /// </summary>
    [HttpPost("{id:long}/characteristics")]
    public async Task<IActionResult> CreateCharacteristic(long id, [FromBody] SaveBodyCharacteristicRequest? request)
    {
        if (request == null)
            return BadRequest(new { message = "Dữ liệu đặc điểm nhận dạng không hợp lệ." });

        var created = await service.ThemDacDiemNhanDangAsync(id, request);
        return Created($"/api/bodies/{id}/characteristics/{created.MaDacDiem}", new
        {
            message = "Thêm đặc điểm nhận dạng thành công.",
            data = created
        });
    }

    /// <summary>
    /// Cập nhật một đặc điểm nhận dạng đã có của hồ sơ thi thể (Task 6.7 — Yêu cầu quyền BODY:UPDATE đầy đủ).
    /// </summary>
    [HttpPut("{id:long}/characteristics/{characteristicId:long}")]
    public async Task<IActionResult> UpdateCharacteristic(
        long id, long characteristicId, [FromBody] SaveBodyCharacteristicRequest? request)
    {
        if (request == null)
            return BadRequest(new { message = "Dữ liệu đặc điểm nhận dạng không hợp lệ." });

        var updated = await service.CapNhatDacDiemNhanDangAsync(id, characteristicId, request);
        return Ok(new
        {
            message = "Cập nhật đặc điểm nhận dạng thành công.",
            data = updated
        });
    }

    /// <summary>
    /// Tải ảnh lên cho một hồ sơ thi thể (Yêu cầu FILE:CREATE và BODY:VIEW).
    /// Ảnh gắn hồ sơ thi thể luôn bị coi là ảnh nhạy cảm nên bắt buộc chuyển vào vùng lưu trữ riêng.
    /// </summary>
    [HttpPost("{id:long}/images")]
    // Ngân sách request phải lớn hơn trần dung lượng ảnh, nếu không ASP.NET cắt luồng trước
    // và người dùng nhận lỗi binding 400 thay vì thông báo 413 của hệ thống.
    [RequestSizeLimit(FileHelper.MaxFileSizeInBytes * 2)]
    public async Task<IActionResult> UploadImage(
        long id, [FromForm] IFormFile? file, [FromForm] string? loaiHinhAnh, [FromForm] string? moTa)
    {
        if (images is null)
            throw new InvalidOperationException("Dịch vụ hình ảnh chưa được cấu hình.");

        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Vui lòng chọn tệp ảnh." });

        if (file.Length > FileHelper.MaxFileSizeInBytes)
            return StatusCode(StatusCodes.Status413PayloadTooLarge,
                new { message = $"Ảnh phải lớn hơn 0 và không quá {FileHelper.MaxFileSizeInBytes / (1024 * 1024)} MB." });

        if (moTa is not null && moTa.Length > 500)
            return BadRequest(new { message = "Mô tả ảnh tối đa 500 ký tự." });

        // Chặn ghi tệp trước khi biết hồ sơ có tồn tại và người gọi xem được nó hay không
        var hoSo = await service.LayChiTietAsync(id);
        if (hoSo is null)
            return NotFound(new { message = $"Không tìm thấy hồ sơ thi thể #{id}." });

        var req = new UploadImageRequest
        {
            MaHoSoThiThe = id,   // mã hồ sơ luôn lấy từ đường dẫn, không tin dữ liệu gửi lên
            LoaiHinhAnh = string.IsNullOrWhiteSpace(loaiHinhAnh) ? ImageHelper.TypeBodyExamination : loaiHinhAnh.Trim(),
            MoTa = string.IsNullOrWhiteSpace(moTa) ? null : moTa.Trim()
        };

        await using var stream = file.OpenReadStream();
        var created = await images.TaiLenHinhAnhTuStreamAsync(stream, file.FileName, req);

        return CreatedAtAction(nameof(GetImage), new { id, imageId = created.MaHinhAnh }, (object)created);
    }

    /// <summary>
    /// Đọc tệp ảnh của hồ sơ thi thể. Bắt buộc JWT và quyền xem ảnh; không tồn tại đường dẫn công khai
    /// nên trình duyệt không thể lấy ảnh nếu không qua kiểm soát truy cập (NFR1.2).
    /// </summary>
    [HttpGet("{id:long}/images/{imageId:long}")]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> GetImage(long id, long imageId)
    {
        if (images is null)
            throw new InvalidOperationException("Dịch vụ hình ảnh chưa được cấu hình.");

        var ketQua = await images.DocAnhCuaHoSoThiTheAsync(id, imageId);
        if (ketQua is null)
            return NotFound(new { message = "Không tìm thấy ảnh hoặc bạn không có quyền xem ảnh này." });

        return File(ketQua.Value.DuLieu, ketQua.Value.DinhDang);
    }
}
