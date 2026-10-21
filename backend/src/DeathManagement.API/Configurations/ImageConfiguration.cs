using DeathManagement.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Image = DeathManagement.API.Entities.Image;

namespace DeathManagement.API.Configurations;

public class ImageConfiguration : IEntityTypeConfiguration<Image>
{
    public void Configure(EntityTypeBuilder<Image> b)
    {
        b.ToTable("HinhAnh");
        b.HasKey(x => x.MaHinhAnh);

        b.Property(x => x.TenFile).HasMaxLength(255).IsRequired();
        b.Property(x => x.TenGoc).HasMaxLength(255).IsRequired();
        b.Property(x => x.DuongDan).HasMaxLength(500).IsRequired();
        b.Property(x => x.LoaiHinhAnh).HasMaxLength(50).IsRequired();
        b.Property(x => x.DinhDang).HasMaxLength(30).IsRequired();
        b.Property(x => x.HashMD5).HasMaxLength(64);
        b.Property(x => x.MoTa).HasMaxLength(500);
        b.Property(x => x.TrangThai).HasMaxLength(20).IsRequired();

        // Foreign Key
        b.HasOne(x => x.TaiKhoanTaiLen)
         .WithMany()
         .HasForeignKey(x => x.MaTaiKhoanTaiLen)
         .OnDelete(DeleteBehavior.Restrict);
    }
}
