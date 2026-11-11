namespace DeathManagement.API.DTOs.Role;

public class PermissionDto
{
    public int MaQuyen { get; set; }
    public string Resource { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string TenQuyen { get; set; } = string.Empty;
    public string? MoTa { get; set; }
    public string PhamVi { get; set; } = "ALL";
}

public class ResourceGroupedPermissionsDto
{
    public string Resource { get; set; } = string.Empty;
    public List<PermissionDto> Permissions { get; set; } = new();
}
