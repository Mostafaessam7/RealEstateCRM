using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealEstateCRM.Domain.Entities;

namespace RealEstateCRM.Infrastructure.Persistence.Configurations;

public class UnitImageConfiguration : IEntityTypeConfiguration<UnitImage>
{
    public void Configure(EntityTypeBuilder<UnitImage> builder)
    {
        builder.ToTable("UnitImages");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.BlobPath).IsRequired().HasMaxLength(500);
        builder.Property(i => i.Url).IsRequired().HasMaxLength(1000);
        builder.Property(i => i.FileName).IsRequired().HasMaxLength(260);
        builder.Property(i => i.ContentType).IsRequired().HasMaxLength(100);

        builder.HasIndex(i => new { i.CompanyId, i.UnitId });

        builder.HasOne<Unit>()
            .WithMany()
            .HasForeignKey(i => i.UnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
