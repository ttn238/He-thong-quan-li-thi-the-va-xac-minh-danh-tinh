namespace DeathManagement.API.DTOs.Ai;

public class AiImageDto
{
    public string? FileName { get; set; }
    public long? SizeBytes { get; set; }
    public string? Format { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public string? Url { get; set; }
}

public class AiOcrItem
{
    public string Text { get; set; } = "";
    public string? Kind { get; set; }          // street_sign | license_plate | other
    public double Confidence { get; set; }     // 0..1
    public int[]? Bbox { get; set; }           // [x, y, rộng, cao]
    public string? Note { get; set; }
}

public class AiObjectItem
{
    public string Label { get; set; } = "";
    public string? Description { get; set; }
    public double Confidence { get; set; }
    public int[]? Bbox { get; set; }
    public string? Tag { get; set; }
}

public class AiLocationItem
{
    public int Rank { get; set; }
    public string Name { get; set; } = "";
    public double Confidence { get; set; }
    public string? AdminArea { get; set; }
    public double? Lat { get; set; }
    public double? Lng { get; set; }
    public string? Basis { get; set; }
}

public class AiMissingField
{
    public string Field { get; set; } = "";
    public string Status { get; set; } = "missing";   // done | missing | out_of_scope | unsupported
    public string? Value { get; set; }
    public string? Recommendation { get; set; }
}

public class AiErrorDto
{
    public string? Code { get; set; }
    public string? Message { get; set; }
    public int? HttpStatus { get; set; }
    public string? TraceId { get; set; }
    public string? OccurredAt { get; set; }
    public string? Endpoint { get; set; }
}

public class AiReviewDto
{
    public string Decision { get; set; } = "confirm";   // confirm | need_more | reject
    public List<string> CheckedItems { get; set; } = new();
    public string Note { get; set; } = "";
    public string? Reviewer { get; set; }
    public string? ReviewedAt { get; set; }
}

public class AiResultDto
{
    public string RequestId { get; set; } = "";
    public string Status { get; set; } = "processing";  // processing | retrying | completed | failed
    public int? Progress { get; set; }
    public int? Attempt { get; set; }
    public string? LinkedRecordId { get; set; }
    public string? SubmittedAt { get; set; }
    public string? CompletedAt { get; set; }
    public int? DurationMs { get; set; }
    public string? Engine { get; set; }
    public string? ImageHash { get; set; }
    public AiImageDto? Image { get; set; }
    public List<AiOcrItem> Ocr { get; set; } = new();
    public List<AiObjectItem> Objects { get; set; } = new();
    public List<AiLocationItem> Locations { get; set; } = new();
    public List<AiMissingField> MissingFields { get; set; } = new();
    public AiErrorDto? Error { get; set; }
    public AiReviewDto? Review { get; set; }

    /// <summary>JSON có thể chứa null / thiếu mảng: chuẩn hóa để giao diện không bao giờ gặp null.</summary>
    public AiResultDto Normalize()
    {
        RequestId ??= "";
        Status ??= "processing";
        Ocr ??= new();
        Objects ??= new();
        Locations ??= new();
        MissingFields ??= new();
        return this;
    }
}

public enum AiViewState { Loading, Processing, Retrying, Success, Partial, Empty, Error }

public static class AiViewHelper
{
    /// <summary>Suy ra trạng thái hiển thị (cùng quy tắc với layViewState của frontend React).</summary>
    public static AiViewState Get(AiResultDto? data, bool loading, bool hasError)
    {
        if (loading) return AiViewState.Loading;
        if (hasError || data == null || data.Status == "failed") return AiViewState.Error;
        if (data.Status == "retrying") return AiViewState.Retrying;
        if (data.Status == "processing") return AiViewState.Processing;
        if (data.Ocr.Count + data.Objects.Count + data.Locations.Count == 0) return AiViewState.Empty;
        return data.MissingFields.Any(f => f.Status != "done") ? AiViewState.Partial : AiViewState.Success;
    }
}