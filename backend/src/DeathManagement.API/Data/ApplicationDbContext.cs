using DeathManagement.API.Entities;
using Microsoft.EntityFrameworkCore;
using DeathManagement.API.Entities;
using Microsoft.EntityFrameworkCore;
using Image = DeathManagement.API.Entities.Image;

namespace DeathManagement.API.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Image> Images => Set<Image>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        mb.Entity<Role>(e =>
        {
            e.ToTable("VaiTro");
            e.HasKey(x => x.MaVaiTro);
            e.Ignore(x => x.MaCode);
            e.Ignore(x => x.MoTa);
            e.Ignore(x => x.TrangThai);
            e.Ignore(x => x.NgayTao);
        });
    }
}
