using DeathManagement.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeathManagement.API.Configurations;

public class StorageLocationConfiguration : IEntityTypeConfiguration<StorageLocation>
{
    public void Configure(EntityTypeBuilder<StorageLocation> b)
    {
        b.ToTable("ViTriLuuTru");
        b.HasKey(x => x.MaViTri);

        b.Property(x => x.Khu).HasMaxLength(50).IsRequired();
        b.Property(x => x.Tu).HasMaxLength(50).IsRequired();
        b.Property(x => x.Ngan).HasMaxLength(50).IsRequired();
        b.Property(x => x.SucChua).IsRequired().HasDefaultValue(1);

        // Ràng buộc duy nhất: Khu - Tu - Ngan (UQ_ViTriLuuTru_Khu_Tu_Ngan)
        b.HasIndex(x => new { x.Khu, x.Tu, x.Ngan })
         .IsUnique()
         .HasDatabaseName("UQ_ViTriLuuTru_Khu_Tu_Ngan");
    }
}
