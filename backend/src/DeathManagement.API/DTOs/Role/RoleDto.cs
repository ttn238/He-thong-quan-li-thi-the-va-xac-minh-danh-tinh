namespace DeathManagement.API.DTOs.Role;

public class RoleDto
{
    public int MaVaiTro { get; set; }
    public string TenVaiTro { get; set; } = string.Empty;
    public string MaCode { get; set; } = string.Empty;
    public string? MoTa { get; set; }
    public int UserCount { get; set; }
    public List<PermissionDto> Permissions { get; set; } = new();
}
