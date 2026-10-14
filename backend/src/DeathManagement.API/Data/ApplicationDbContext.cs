<<<<<<< Updated upstream
=======
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
        //mb.Entity<Role>(e => { e.ToTable("VaiTro"); e.HasKey(x => x.MaVaiTro); });
        mb.Entity<Role>(e =>
        {
            e.ToTable("VaiTro");
            e.HasKey(x => x.MaVaiTro);
            // Bảng VaiTro trong SQL chỉ có MaVaiTro, TenVaiTro -> bỏ các cột không tồn tại
            e.Ignore(x => x.MaCode);
            e.Ignore(x => x.MoTa);
            e.Ignore(x => x.TrangThai);
            e.Ignore(x => x.NgayTao);
        });
    }
}
>>>>>>> Stashed changes
