using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealEstateCRM.Domain.Entities;

namespace RealEstateCRM.Infrastructure.Persistence.Configurations;

public class CampaignRecipientConfiguration : IEntityTypeConfiguration<CampaignRecipient>
{
    public void Configure(EntityTypeBuilder<CampaignRecipient> builder)
    {
        builder.ToTable("CampaignRecipients");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.ErrorMessage).HasMaxLength(500);

        builder.HasIndex(r => new { r.CompanyId, r.CampaignId });

        builder.HasOne<Campaign>()
            .WithMany()
            .HasForeignKey(r => r.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Lead>()
            .WithMany()
            .HasForeignKey(r => r.LeadId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
