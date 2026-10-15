using DeathManagement.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeathManagement.API.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("TaiKhoanNguoiDung", t => t.UseSqlOutputClause(false));
        b.HasKey(x => x.MaTaiKhoan);
        b.HasOne(x => x.Role)
            .WithMany(x => x.Users)
            .HasForeignKey(x => x.MaVaiTro)
            .HasConstraintName("FK_TaiKhoan_VaiTro");
    }
}

