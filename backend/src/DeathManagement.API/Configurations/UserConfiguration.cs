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

        b.Property(x => x.TenDangNhap).HasMaxLength(50).IsRequired();
        b.Property(x => x.MatKhauHash).HasMaxLength(255).IsRequired();
        b.Property(x => x.HoTen).HasMaxLength(150).IsRequired();
        b.Property(x => x.Email).HasMaxLength(150);
        b.Property(x => x.SoDienThoai).HasMaxLength(15);
        b.Property(x => x.CCCD).HasMaxLength(20);
        b.Property(x => x.DonVi).HasMaxLength(150);
        b.Property(x => x.ChucVu).HasMaxLength(100);
        b.Property(x => x.TrangThai).HasMaxLength(20).IsRequired().HasDefaultValue("HoatDong");
        b.Property(x => x.SoLanDangNhapSai).HasDefaultValue(0);
        b.Property(x => x.NgayTao).HasDefaultValueSql("SYSDATETIME()");
        b.Property(x => x.NgayCapNhat).HasDefaultValueSql("SYSDATETIME()");

        b.HasOne(x => x.Role)
         .WithMany(x => x.Users)
         .HasForeignKey(x => x.MaVaiTro)
         .HasConstraintName("FK_TaiKhoan_VaiTro");
    }
}
