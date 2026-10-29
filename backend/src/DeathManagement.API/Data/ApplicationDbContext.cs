using DeathManagement.API.Entities;
using Microsoft.EntityFrameworkCore;
using Image = DeathManagement.API.Entities.Image;

namespace DeathManagement.API.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Image> Images => Set<Image>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
