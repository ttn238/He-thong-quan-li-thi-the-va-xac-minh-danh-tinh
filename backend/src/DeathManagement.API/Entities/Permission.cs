using System.ComponentModel.DataAnnotations.Schema;

namespace DeathManagement.API.Entities;

/// <summary>
/// Represents a system permission (Resource x Action) from table QuyenHeThong.
/// </summary>
public class Permission
{
    public int MaQuyen { get; set; }
    public string Resource { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;

    [NotMapped]
    public string TenQuyen => $"{Resource}:{Action}";

    [NotMapped]
    public string? MoTa { get; set; }

    // Navigation property
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
