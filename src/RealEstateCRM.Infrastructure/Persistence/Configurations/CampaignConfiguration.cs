using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealEstateCRM.Domain.Entities;
using RealEstateCRM.Infrastructure.Identity;

namespace RealEstateCRM.Infrastructure.Persistence.Configurations;

public class CampaignConfiguration : IEntityTypeConfiguration<Campaign>
{
    public void Configure(EntityTypeBuilder<Campaign> builder)
    {
        builder.ToTable("Campaigns");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).HasMaxLength(150).IsRequired();
        builder.Property(c => c.Subject).HasMaxLength(200);
        builder.Property(c => c.Body).HasMaxLength(4000).IsRequired();
        builder.Property(c => c.Channel).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.TargetStatus).HasConversion<string>().HasMaxLength(30);
        builder.Property(c => c.TargetSource).HasConversion<string>().HasMaxLength(30);

        builder.HasIndex(c => new { c.CompanyId, c.Status });

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(c => c.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
