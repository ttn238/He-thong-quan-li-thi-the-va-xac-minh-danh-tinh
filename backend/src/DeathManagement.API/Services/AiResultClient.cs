using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using DeathManagement.API.DTOs.Ai;

namespace DeathManagement.API.Services;

/// <summary>Lỗi khi gọi ai-service (mất kết nối, quá thời gian, hoặc ai-service trả lỗi).</summary>
public class AiServiceException : Exception
{
    public string Code { get; }
    public int? HttpStatus { get; }
    public string TraceId { get; }
    public string Endpoint { get; }
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.Now;

    public AiServiceException(string code, string message, string endpoint, int? httpStatus = null,
        string? traceId = null, Exception? inner = null) : base(message, inner)
    {
        Code = code;
        Endpoint = endpoint;
        HttpStatus = httpStatus;
        TraceId = string.IsNullOrWhiteSpace(traceId) ? "—" : traceId!;
    }
}

/// <summary>Máy khách HTTP gọi ai-service: /v1/results (phân tích ảnh, xem kết quả, lưu thẩm định).</summary>
public static class AiResultClient
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);
    private static readonly Lazy<HttpClient> Http = new(() => new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(Math.Max(5, Program.AiTimeoutSeconds))
    });

    private static readonly SemaphoreSlim AutoStartLock = new(1, 1);
    private static Process? _localAiProcess;

    public static string BaseUrl => Program.AiBaseUrl.TrimEnd('/');

    private static async Task<bool> PingOnceAsync(CancellationToken ct = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(3));
            using var res = await Http.Value.GetAsync(BaseUrl + "/health", cts.Token);
            return res.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public static async Task<bool> PingAsync(CancellationToken ct = default)
    {
        if (await PingOnceAsync(ct)) return true;
        return await EnsureLocalServerRunningAsync(ct);
    }

    private static async Task<bool> EnsureLocalServerRunningAsync(CancellationToken ct = default)
    {
        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri) ||
            (uri.Host != "localhost" && uri.Host != "127.0.0.1"))
            return false;

        await AutoStartLock.WaitAsync(ct);
        try
        {
            if (await PingOnceAsync(ct)) return true;

            if (_localAiProcess == null || _localAiProcess.HasExited)
            {
                var aiDir = FindAiServiceDirectory();
                if (aiDir == null) return false;

                var venvPython = Path.Combine(aiDir, ".venv", "Scripts", "python.exe");
                var pythonExe = File.Exists(venvPython) ? venvPython : "python";
                var mainPy = Path.Combine(aiDir, "main.py");
                if (!File.Exists(mainPy)) return false;

                var psi = new ProcessStartInfo
                {
                    FileName = pythonExe,
                    Arguments = $"-m uvicorn main:app --host 127.0.0.1 --port {uri.Port}",
                    WorkingDirectory = aiDir,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                _localAiProcess = Process.Start(psi);
            }

            // Chờ uvicorn + preload_models khởi động xong (tối đa ~15 giây)
            for (int i = 0; i < 30; i++)
            {
                await Task.Delay(500, ct);
                if (await PingOnceAsync(ct)) return true;
                if (_localAiProcess is { HasExited: true }) break;
            }

            return await PingOnceAsync(ct);
        }
        catch
        {
            return false;
        }
        finally
        {
            AutoStartLock.Release();
        }
    }

    private static string? FindAiServiceDirectory()
    {
        foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            var dir = new DirectoryInfo(start);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "ai-service");
                if (File.Exists(Path.Combine(candidate, "main.py")))
                    return candidate;
                dir = dir.Parent;
            }
        }
        return null;
    }

    public static async Task<AiResultDto> AnalyzeAsync(string filePath, string? linkedRecordId, CancellationToken ct = default)
    {
        const string path = "/v1/results";
        byte[] bytes;
        try { bytes = await File.ReadAllBytesAsync(filePath, ct); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new AiServiceException("ERR_AI_FILE", "Không đọc được tệp ảnh: " + ex.Message, BaseUrl + path, null, null, ex);
        }

        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(MimeOf(filePath));
        form.Add(file, "file", Path.GetFileName(filePath));
        if (!string.IsNullOrWhiteSpace(linkedRecordId)) form.Add(new StringContent(linkedRecordId), "linked_record_id");

        var node = await SendAsync(() => Http.Value.PostAsync(BaseUrl + path, form, ct), path, ct);
        return ToResult(node);
    }

    public static async Task<AiResultDto> GetAsync(string requestId, CancellationToken ct = default)
    {
        var path = "/v1/results/" + Uri.EscapeDataString(requestId);
        var node = await SendAsync(() => Http.Value.GetAsync(BaseUrl + path, ct), path, ct);
        return ToResult(node);
    }

    /// <summary>Tải ảnh gốc; <paramref name="url"/> là đường dẫn tương đối ai-service trả về (/v1/results/{id}/image).</summary>
    public static async Task<byte[]> DownloadAsync(string url, CancellationToken ct = default)
    {
        var full = url.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? url : BaseUrl + (url.StartsWith('/') ? url : "/" + url);
        try
        {
            using var res = await Http.Value.GetAsync(full, ct);
            if (!res.IsSuccessStatusCode)
                throw new AiServiceException("ERR_AI_HTTP", $"Không tải được ảnh (HTTP {(int)res.StatusCode}).", full, (int)res.StatusCode);
            return await res.Content.ReadAsByteArrayAsync(ct);
        }
        catch (AiServiceException) { throw; }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) { throw Wrap(ex, full); }
    }

    public static async Task<AiReviewDto> SaveReviewAsync(string requestId, AiReviewDto review, CancellationToken ct = default)
    {
        var path = $"/v1/results/{Uri.EscapeDataString(requestId)}/review";
        var body = new JsonObject
        {
            ["decision"] = review.Decision,
            ["checkedItems"] = new JsonArray(review.CheckedItems.Select(x => (JsonNode?)JsonValue.Create(x)).ToArray()),
            ["note"] = review.Note,
            ["reviewer"] = review.Reviewer
        };
        using var content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
        var node = await SendAsync(() => Http.Value.PostAsync(BaseUrl + path, content, ct), path, ct);
        return new AiReviewDto
        {
            Decision = Str(node, "decision") ?? review.Decision,
            Note = Str(node, "note") ?? review.Note,
            Reviewer = Str(node, "reviewer") ?? review.Reviewer,
            ReviewedAt = Str(node, "reviewedAt"),
            CheckedItems = node["checkedItems"] is JsonArray a
                ? a.Select(x => x?.GetValue<string>() ?? "").ToList()
                : review.CheckedItems
        };
    }

    // ===== Nội bộ =====
    private static async Task<JsonNode> SendAsync(Func<Task<HttpResponseMessage>> send, string path, CancellationToken ct)
    {
        var endpoint = BaseUrl + path;
        try
        {
            await EnsureLocalServerRunningAsync(ct);
            using var res = await send();
            var text = await res.Content.ReadAsStringAsync(ct);
            if (!res.IsSuccessStatusCode) throw FromError(res, text, endpoint);
            return JsonNode.Parse(text) ?? throw new AiServiceException("ERR_AI_BAD_RESPONSE", "Dịch vụ AI trả dữ liệu rỗng.", endpoint, (int)res.StatusCode);
        }
        catch (AiServiceException) { throw; }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) { throw Wrap(ex, endpoint); }
    }

    private static AiServiceException Wrap(Exception ex, string endpoint) => ex switch
    {
        TaskCanceledException or OperationCanceledException =>
            new AiServiceException("ERR_AI_TIMEOUT", "Dịch vụ AI không phản hồi kịp thời gian cho phép.", endpoint, null, null, ex),
        HttpRequestException =>
            new AiServiceException("ERR_AI_UNREACHABLE", "Không kết nối được dịch vụ AI (" + BaseUrl + "). Hãy kiểm tra ai-service đã chạy chưa.", endpoint, null, null, ex),
        JsonException =>
            new AiServiceException("ERR_AI_BAD_RESPONSE", "Dịch vụ AI trả dữ liệu không đúng định dạng.", endpoint, null, null, ex),
        _ => new AiServiceException("ERR_AI_UNKNOWN", ex.Message, endpoint, null, null, ex)
    };

    private static AiServiceException FromError(HttpResponseMessage res, string text, string endpoint)
    {
        string code = "ERR_AI_HTTP", msg = $"Dịch vụ AI trả lỗi HTTP {(int)res.StatusCode}.";
        string? trace = null;
        try
        {
            var n = JsonNode.Parse(text);
            if (n?["error"] is JsonNode e)
            {
                code = Str(e, "code") ?? code;
                msg = Str(e, "message") ?? msg;
                trace = Str(n, "request_id");
            }
            else if (Str(n, "detail") is string d) msg = d;
        }
        catch (JsonException) { /* giữ thông báo mặc định */ }
        return new AiServiceException(code, msg, endpoint, (int)res.StatusCode, trace);
    }

    private static string? Str(JsonNode? n, string key) => n is JsonObject o && o[key] is JsonValue v && v.TryGetValue<string>(out var s) ? s : null;

    private static int[]? Box(JsonNode? b)
    {
        if (b is not JsonObject o) return null;
        int G(string k) => o[k] is JsonValue v && v.TryGetValue<int>(out var i) ? i : 0;
        int x1 = G("xMin"), y1 = G("yMin"), x2 = G("xMax"), y2 = G("yMax");
        return new[] { x1, y1, Math.Max(0, x2 - x1), Math.Max(0, y2 - y1) };   // [x, y, rộng, cao]
    }

    private static AiResultDto ToResult(JsonNode n)
    {
        var r = new AiResultDto
        {
            RequestId = Str(n, "requestId") ?? "",
            Status = Str(n, "status") ?? "processing",
            LinkedRecordId = Str(n, "linkedRecordId"),
            SubmittedAt = Str(n, "submittedAt"),
            CompletedAt = Str(n, "completedAt"),
            DurationMs = n["durationMs"]?.GetValue<int?>(),
            Engine = Str(n, "engine"),
            ImageHash = Str(n, "imageHash"),
            Image = n["image"] is JsonNode im ? im.Deserialize<AiImageDto>(JsonOpts) : null
        };

        foreach (var o in n["ocr"]?.AsArray() ?? new JsonArray())
            r.Ocr.Add(new AiOcrItem
            {
                Text = Str(o, "text") ?? "",
                Kind = Str(o, "kind"),
                Confidence = o?["confidence"]?.GetValue<double>() ?? 0,
                Bbox = Box(o?["bbox"]),
                Note = Str(o, "note")
            });

        foreach (var o in n["objects"]?.AsArray() ?? new JsonArray())
        {
            var attrs = o?["attributes"] is JsonObject ao
                ? string.Join(", ", ao.Select(kv => $"{kv.Key}: {kv.Value}"))
                : null;
            r.Objects.Add(new AiObjectItem
            {
                Label = Str(o, "label") ?? "",
                Description = string.IsNullOrEmpty(attrs) ? null : attrs,
                Confidence = o?["confidence"]?.GetValue<double>() ?? 0,
                Bbox = Box(o?["box"]),
                Tag = Str(o, "category")
            });
        }

        foreach (var o in n["locations"]?.AsArray() ?? new JsonArray())
            if (o != null && o.Deserialize<AiLocationItem>(JsonOpts) is { } loc) r.Locations.Add(loc);

        foreach (var o in n["missingFields"]?.AsArray() ?? new JsonArray())
            if (o != null && o.Deserialize<AiMissingField>(JsonOpts) is { } mf) r.MissingFields.Add(mf);

        if (n["error"] is JsonObject er)
            r.Error = new AiErrorDto { Code = Str(er, "code"), Message = Str(er, "message") };

        if (n["review"] is JsonObject rv)
            r.Review = new AiReviewDto
            {
                Decision = Str(rv, "decision") ?? "confirm",
                Note = Str(rv, "note") ?? "",
                Reviewer = Str(rv, "reviewer"),
                ReviewedAt = Str(rv, "reviewedAt"),
                CheckedItems = rv["checkedItems"] is JsonArray ci ? ci.Select(x => x?.GetValue<string>() ?? "").ToList() : new()
            };

        return r.Normalize();
    }

    private static string MimeOf(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".webp" => "image/webp",
        ".bmp" => "image/bmp",
        _ => "image/jpeg"
    };
}
