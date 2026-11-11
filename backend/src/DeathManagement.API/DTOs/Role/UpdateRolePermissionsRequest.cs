using System.ComponentModel.DataAnnotations;

namespace DeathManagement.API.DTOs.Role;

public class UpdateRolePermissionsRequest
{
    [Required]
    public List<RolePermissionItemRequest> Permissions { get; set; } = new();
}

public class RolePermissionItemRequest
{
    [Required]
    public int MaQuyen { get; set; }

    [Required]
    [RegularExpression("^(ALL|OWN|PARTIAL)$", ErrorMessage = "Phạm vi phải là 'ALL', 'OWN', hoặc 'PARTIAL'.")]
    public string PhamVi { get; set; } = "ALL";
}
