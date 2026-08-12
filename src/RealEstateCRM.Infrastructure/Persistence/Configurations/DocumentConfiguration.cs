using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealEstateCRM.Domain.Entities;
using RealEstateCRM.Infrastructure.Identity;

namespace RealEstateCRM.Infrastructure.Persistence.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("Documents");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.BlobPath).IsRequired().HasMaxLength(500);
        builder.Property(d => d.Url).IsRequired().HasMaxLength(1000);
        builder.Property(d => d.FileName).IsRequired().HasMaxLength(260);
        builder.Property(d => d.ContentType).IsRequired().HasMaxLength(100);

        builder.HasIndex(d => new { d.CompanyId, d.LeadId });
        builder.HasIndex(d => new { d.CompanyId, d.DealId });

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(d => d.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Lead>()
            .WithMany()
            .HasForeignKey(d => d.LeadId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Deal>()
            .WithMany()
            .HasForeignKey(d => d.DealId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
