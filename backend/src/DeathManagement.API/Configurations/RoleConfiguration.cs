using DeathManagement.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeathManagement.API.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> b)
    {
        b.ToTable("VaiTro");
        b.HasKey(x => x.MaVaiTro);

        b.Property(x => x.TenVaiTro)
         .HasMaxLength(100)
         .IsRequired();

        b.Ignore(x => x.MaCode);
        b.Ignore(x => x.MoTa);
        b.Ignore(x => x.TrangThai);
        b.Ignore(x => x.NgayTao);

        b.HasMany(x => x.Users)
         .WithOne(x => x.Role)
         .HasForeignKey(x => x.MaVaiTro);

        b.HasMany(x => x.RolePermissions)
         .WithOne(x => x.Role)
         .HasForeignKey(x => x.MaVaiTro);
    }
}

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> b)
    {
        b.ToTable("QuyenHeThong");
        b.HasKey(x => x.MaQuyen);

        b.Property(x => x.Resource)
         .HasMaxLength(80)
         .IsRequired();

        b.Property(x => x.Action)
         .HasMaxLength(30)
         .IsRequired();

        b.HasMany(x => x.RolePermissions)
         .WithOne(x => x.Permission)
         .HasForeignKey(x => x.MaQuyen);
    }
}

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> b)
    {
        b.ToTable("VaiTro_Quyen");
        b.HasKey(x => new { x.MaVaiTro, x.MaQuyen });

        b.Property(x => x.PhamVi)
         .HasMaxLength(10)
         .IsRequired()
         .HasDefaultValue("ALL");

        b.HasOne(x => x.Role)
         .WithMany(x => x.RolePermissions)
         .HasForeignKey(x => x.MaVaiTro);

        b.HasOne(x => x.Permission)
         .WithMany(x => x.RolePermissions)
         .HasForeignKey(x => x.MaQuyen);
    }
}
