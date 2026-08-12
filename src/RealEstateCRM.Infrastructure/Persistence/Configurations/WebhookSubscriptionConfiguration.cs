using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealEstateCRM.Domain.Entities;
using RealEstateCRM.Infrastructure.Identity;

namespace RealEstateCRM.Infrastructure.Persistence.Configurations;

public class WebhookSubscriptionConfiguration : IEntityTypeConfiguration<WebhookSubscription>
{
    public void Configure(EntityTypeBuilder<WebhookSubscription> builder)
    {
        builder.ToTable("WebhookSubscriptions");
        builder.HasKey(w => w.Id);

        builder.Property(w => w.Url).HasMaxLength(500).IsRequired();
        builder.Property(w => w.Secret).HasMaxLength(200).IsRequired();
        builder.Property(w => w.EventTypes).HasMaxLength(500).IsRequired();

        builder.HasIndex(w => new { w.CompanyId, w.IsActive });

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(w => w.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
