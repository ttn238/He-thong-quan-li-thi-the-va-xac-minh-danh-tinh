namespace DeathManagement.API.DTOs.User;

/// <summary>
/// Tiêu chí tìm kiếm, lọc và phân trang danh sách người dùng
/// </summary>
public class UserFilterRequest
{
    public string? Keyword { get; set; }
    public int? MaVaiTro { get; set; }
    public string? TrangThai { get; set; } // HoatDong, Khoa, KhoaTam
    public string? DonVi { get; set; }

    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
