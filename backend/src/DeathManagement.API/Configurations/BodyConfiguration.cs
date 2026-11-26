using DeathManagement.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeathManagement.API.Configurations;

/// <summary>
/// EF Core fluent mapping cho bảng HoSoThiThe và bảng NhatKyHeThong (lịch sử trạng thái).
/// </summary>
public class BodyConfiguration : IEntityTypeConfiguration<Body>
{
    public void Configure(EntityTypeBuilder<Body> b)
    {
        b.ToTable("HoSoThiThe", t =>
        {
            t.HasTrigger("trg_HoSoThiThe_KiemTra");
            t.HasTrigger("trg_HoSoThiThe_SauDong");
        });
        b.HasKey(x => x.MaHoSoThiThe);

        b.Property(x => x.ThoiGianPhatHien).IsRequired();
        b.Property(x => x.DiaDiem).HasMaxLength(255);
        b.Property(x => x.TrangThai).HasMaxLength(30).IsRequired()
            .HasDefaultValue(Body.StatusMoiTiepNhan);
        b.Property(x => x.GioiTinh).HasMaxLength(20);
        b.Property(x => x.ChieuCao).HasPrecision(5, 2);
        b.Property(x => x.DacDiemCoThe).HasColumnType("nvarchar(max)");
        b.Property(x => x.KhuonMat).HasColumnType("varbinary(max)");
    }
}

public class BodyCharacteristicConfiguration : IEntityTypeConfiguration<BodyCharacteristic>
{
    public void Configure(EntityTypeBuilder<BodyCharacteristic> b)
    {
        b.ToTable("DacDiemNhanDang");
        b.HasKey(x => x.MaDacDiem);

        b.Property(x => x.MaDacDiem).ValueGeneratedOnAdd();
        b.Property(x => x.MaHoSoThiThe).IsRequired();
        b.Property(x => x.LoaiDacDiem).HasMaxLength(50).IsRequired();
        b.Property(x => x.ViTriCoThe).HasMaxLength(150);
        b.Property(x => x.MoTa).HasMaxLength(500).IsRequired();
        b.Property(x => x.NgayGhiNhan).IsRequired();

        b.HasOne(x => x.HoSoThiThe)
            .WithMany(h => h.DacDiemNhanDangs)
            .HasForeignKey(x => x.MaHoSoThiThe)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("NhatKyHeThong", t => t.HasTrigger("trg_NhatKy_ChiThem"));
        b.HasKey(x => x.MaNhatKy);

        b.Property(x => x.HanhDong).HasMaxLength(100).IsRequired();
        b.Property(x => x.ThoiGian).IsRequired();
        b.Property(x => x.DoiTuongTacDong).HasMaxLength(255);

        b.HasOne(x => x.TaiKhoan)
            .WithMany()
            .HasForeignKey(x => x.MaTaiKhoan)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
