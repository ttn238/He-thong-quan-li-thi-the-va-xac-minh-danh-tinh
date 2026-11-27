using DeathManagement.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeathManagement.API.Configurations;

/// <summary>
/// EF Core fluent mapping cho bảng HoSoNguoiMatTich và NguoiThan.
/// </summary>
public class MissingPersonConfiguration : IEntityTypeConfiguration<MissingPerson>
{
    public void Configure(EntityTypeBuilder<MissingPerson> b)
    {
        b.ToTable("HoSoNguoiMatTich");
        b.HasKey(x => x.MaHoSoMatTich);

        b.Property(x => x.MaHoSoMatTich).ValueGeneratedOnAdd();
        b.Property(x => x.HoTen).HasMaxLength(150).IsRequired();
        b.Property(x => x.NgaySinh).HasColumnType("date");
        b.Property(x => x.GioiTinh).HasMaxLength(10);
        b.Property(x => x.NoiMatTich).HasMaxLength(255);
        b.Property(x => x.TrangThai).HasMaxLength(30).IsRequired()
            .HasDefaultValue(MissingPerson.StatusDangTimKiem);
    }
}

public class RelativeConfiguration : IEntityTypeConfiguration<Relative>
{
    public void Configure(EntityTypeBuilder<Relative> b)
    {
        b.ToTable("NguoiThan");
        b.HasKey(x => x.MaNguoiThan);

        b.Property(x => x.MaNguoiThan).ValueGeneratedOnAdd();
        b.Property(x => x.HoTen).HasMaxLength(150).IsRequired();
        b.Property(x => x.QuanHe).HasMaxLength(50);
        b.Property(x => x.SoDienThoai).HasMaxLength(15);
        b.Property(x => x.DiaChi).HasMaxLength(255);
        b.Property(x => x.MaHoSoMatTich).IsRequired();

        b.HasOne(x => x.HoSoMatTich)
            .WithMany(m => m.NguoiThans)
            .HasForeignKey(x => x.MaHoSoMatTich)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
