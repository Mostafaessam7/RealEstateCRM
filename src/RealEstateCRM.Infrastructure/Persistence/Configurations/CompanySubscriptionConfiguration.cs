using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealEstateCRM.Domain.Entities;

namespace RealEstateCRM.Infrastructure.Persistence.Configurations;

public class CompanySubscriptionConfiguration : IEntityTypeConfiguration<CompanySubscription>
{
    public void Configure(EntityTypeBuilder<CompanySubscription> builder)
    {
        builder.ToTable("CompanySubscriptions");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(30);

        // One subscription row per company.
        builder.HasIndex(s => s.CompanyId).IsUnique();

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(s => s.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<SubscriptionPlan>()
            .WithMany()
            .HasForeignKey(s => s.PlanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
