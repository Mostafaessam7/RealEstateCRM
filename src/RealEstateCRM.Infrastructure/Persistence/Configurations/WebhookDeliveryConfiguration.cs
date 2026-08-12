using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealEstateCRM.Domain.Entities;

namespace RealEstateCRM.Infrastructure.Persistence.Configurations;

public class WebhookDeliveryConfiguration : IEntityTypeConfiguration<WebhookDelivery>
{
    public void Configure(EntityTypeBuilder<WebhookDelivery> builder)
    {
        builder.ToTable("WebhookDeliveries");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.EventType).HasMaxLength(100).IsRequired();
        builder.Property(d => d.Payload).HasMaxLength(4000).IsRequired();
        builder.Property(d => d.ErrorMessage).HasMaxLength(500);

        builder.HasIndex(d => new { d.CompanyId, d.WebhookSubscriptionId });

        builder.HasOne<WebhookSubscription>()
            .WithMany()
            .HasForeignKey(d => d.WebhookSubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
