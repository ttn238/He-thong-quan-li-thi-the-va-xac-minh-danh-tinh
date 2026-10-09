using DeathManagement.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace DeathManagement.API.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        mb.Entity<Role>(e => { e.ToTable("VaiTro"); e.HasKey(x => x.MaVaiTro); });
    }
}