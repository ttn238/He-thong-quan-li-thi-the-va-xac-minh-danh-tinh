using System.Net;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeathManagement.ImageApi.Controllers;

[ApiController]
[Route("api/v1/identification/images")]
public sealed class IdentificationController(AiServiceClient ai, ILogger<IdentificationController> logger) : ControllerBase
{
    private const long MaxImageBytes = 10 * 1024 * 1024;
    private static readonly HashSet<string> ImageTypes = new(StringComparer.OrdinalIgnoreCase)
        { "image/jpeg", "image/png", "image/webp", "image/bmp" };

    [HttpPost]
    [Authorize(Policy = "AiAnalyze")]
    [RequestSizeLimit(MaxImageBytes + 1024 * 1024)]
    public async Task<IActionResult> Analyze([FromForm] IFormFile? file, [FromForm] string? linkedRecordId,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0) return BadRequest(new { message = "Vui lòng chọn tệp ảnh." });
        if (file.Length > MaxImageBytes) return StatusCode(413, new { message = "Ảnh vượt quá giới hạn 10 MB." });
        if (!ImageTypes.Contains(file.ContentType)) return BadRequest(new { message = "Chỉ hỗ trợ ảnh JPG, PNG, WEBP hoặc BMP." });
        if (linkedRecordId?.Length > 120) return BadRequest(new { message = "Mã hồ sơ liên kết quá dài." });
        try
        {
            await using var stream = file.OpenReadStream();
            var signature = new byte[12];
            var read = await stream.ReadAsync(signature, cancellationToken);
            stream.Position = 0;
            if (!HasSupportedSignature(signature.AsSpan(0, read), file.ContentType))
                return BadRequest(new { message = "Nội dung tệp không phải ảnh hợp lệ hoặc không khớp định dạng đã chọn." });
            var result = await ai.AnalyzeAsync(stream, file.FileName, file.ContentType, linkedRecordId, cancellationToken);
            return Proxy(result);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(504, new { message = "Dịch vụ AI xử lý quá thời gian cho phép. Vui lòng thử lại." });
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning("AI service is unavailable: {Reason}", ex.Message);
            return StatusCode(503, new { message = "Dịch vụ AI hiện không khả dụng. Vui lòng thử lại sau." });
        }
        catch (TaskCanceledException)
        {
            return StatusCode(504, new { message = "Dịch vụ AI xử lý quá thời gian cho phép. Vui lòng thử lại." });
        }
    }

    [HttpGet("{requestId}")]
    [Authorize(Policy = "AiView")]
    public async Task<IActionResult> GetResult(string requestId, CancellationToken cancellationToken)
        => await ForwardGetAsync($"v1/results/{Uri.EscapeDataString(requestId)}", cancellationToken);

    [HttpGet("{requestId}/image")]
    [Authorize(Policy = "AiView")]
    public async Task<IActionResult> GetImage(string requestId, CancellationToken cancellationToken)
        => await ForwardGetAsync($"v1/results/{Uri.EscapeDataString(requestId)}/image", cancellationToken);

    [HttpPost("{requestId}/review")]
    [Authorize(Policy = "AiReview")]
    public async Task<IActionResult> SaveReview(string requestId, [FromBody] JsonObject? body, CancellationToken cancellationToken)
    {
        if (body is null) return BadRequest(new { message = "Nội dung thẩm định không hợp lệ." });
        body["reviewer"] = User.FindFirst("name")?.Value ?? User.FindFirst("username")?.Value ?? "Cán bộ";
        try
        {
            var result = await ai.ReviewAsync($"v1/results/{Uri.EscapeDataString(requestId)}/review",
                body.ToJsonString(), cancellationToken);
            return Proxy(result);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning("AI service is unavailable: {Reason}", ex.Message);
            return StatusCode(503, new { message = "Dịch vụ AI hiện không khả dụng. Vui lòng thử lại sau." });
        }
        catch (TaskCanceledException)
        {
            return StatusCode(504, new { message = "Dịch vụ AI xử lý quá thời gian cho phép. Vui lòng thử lại." });
        }
    }

    private async Task<IActionResult> ForwardGetAsync(string path, CancellationToken cancellationToken)
    {
        try { return Proxy(await ai.GetAsync(path, cancellationToken)); }
        catch (HttpRequestException ex)
        {
            logger.LogWarning("AI service is unavailable: {Reason}", ex.Message);
            return StatusCode(503, new { message = "Dịch vụ AI hiện không khả dụng. Vui lòng thử lại sau." });
        }
        catch (TaskCanceledException)
        {
            return StatusCode(504, new { message = "Dịch vụ AI xử lý quá thời gian cho phép. Vui lòng thử lại." });
        }
    }

    private IActionResult Proxy(AiProxyResponse response)
    {
        if (response.Status == HttpStatusCode.InternalServerError)
            return StatusCode(502, new { message = "Dịch vụ AI gặp lỗi khi xử lý ảnh." });
        Response.StatusCode = (int)response.Status;
        return File(response.Body, response.ContentType);
    }

    private static bool HasSupportedSignature(ReadOnlySpan<byte> data, string mediaType) => mediaType.ToLowerInvariant() switch
    {
        "image/jpeg" => data.Length >= 3 && data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF,
        "image/png" => data.Length >= 8 && data[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
        "image/webp" => data.Length >= 12 && data[..4].SequenceEqual("RIFF"u8) && data.Slice(8, 4).SequenceEqual("WEBP"u8),
        "image/bmp" => data.Length >= 2 && data[0] == (byte)'B' && data[1] == (byte)'M',
        _ => false,
    };
}
