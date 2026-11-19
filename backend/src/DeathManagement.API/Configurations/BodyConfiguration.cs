using DeathManagement.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeathManagement.API.Configurations;

/// <summary>
/// EF Core fluent mapping cho bảng HoSoThiThe.
/// </summary>
public class BodyConfiguration : IEntityTypeConfiguration<Body>
{
    public void Configure(EntityTypeBuilder<Body> b)
    {
        b.ToTable("HoSoThiThe");
        b.HasKey(x => x.MaHoSoThiThe);

        b.Property(x => x.ThoiGianPhatHien).IsRequired();
        b.Property(x => x.DiaDiem).HasMaxLength(255);
        b.Property(x => x.TrangThai).HasMaxLength(30).IsRequired()
            .HasDefaultValue("MoiTiepNhan");
        b.Property(x => x.GioiTinh).HasMaxLength(10);
        b.Property(x => x.ChieuCao).HasPrecision(5, 2);
        b.Property(x => x.DacDiemCoThe).HasColumnType("nvarchar(max)");
        b.Property(x => x.KhuonMat).HasColumnType("varbinary(max)");
    }
}
