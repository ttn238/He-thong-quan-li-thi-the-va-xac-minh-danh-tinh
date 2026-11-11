namespace DeathManagement.API.Entities;

/// <summary>
/// Represents the link between Role and Permission (table VaiTro_Quyen).
/// Includes data scope (PhamVi: ALL, OWN, PARTIAL).
/// </summary>
public class RolePermission
{
    public int MaVaiTro { get; set; }
    public int MaQuyen { get; set; }
    public string PhamVi { get; set; } = "ALL";

    // Navigation properties
    public virtual Role Role { get; set; } = null!;
    public virtual Permission Permission { get; set; } = null!;
}
