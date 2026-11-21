namespace DeathManagement.API.DTOs.Storage;

public class StorageLocationFilterRequest
{
    public string? Keyword { get; set; }
    public string? Khu { get; set; }
    public string? Tu { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
