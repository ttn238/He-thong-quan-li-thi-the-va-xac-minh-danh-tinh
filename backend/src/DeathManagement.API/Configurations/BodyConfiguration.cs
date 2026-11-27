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
        b.ToTable("HoSoThiThe", t => t.HasTrigger("trg_HoSoThiThe_KiemTra"));
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
