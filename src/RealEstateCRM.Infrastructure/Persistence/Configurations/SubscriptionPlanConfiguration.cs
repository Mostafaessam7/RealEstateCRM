using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealEstateCRM.Domain.Entities;

namespace RealEstateCRM.Infrastructure.Persistence.Configurations;

public class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public static readonly Guid FreePlanId = new("11111111-0000-0000-0000-000000000001");
    public static readonly Guid StarterPlanId = new("11111111-0000-0000-0000-000000000002");
    public static readonly Guid ProPlanId = new("11111111-0000-0000-0000-000000000003");
    public static readonly Guid EnterprisePlanId = new("11111111-0000-0000-0000-000000000004");

    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.ToTable("SubscriptionPlans");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Code).HasMaxLength(30).IsRequired();
        builder.Property(p => p.Name).HasMaxLength(100).IsRequired();
        builder.Property(p => p.MonthlyPrice).HasColumnType("decimal(18,2)");

        builder.HasIndex(p => p.Code).IsUnique();

        var seededAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        builder.HasData(
            new SubscriptionPlan { Id = FreePlanId, Code = "free", Name = "Free", MonthlyPrice = 0, MaxUsers = 3, MaxLeads = 100, MaxUnits = 25, IsActive = true, CreatedAt = seededAt, UpdatedAt = seededAt },
            new SubscriptionPlan { Id = StarterPlanId, Code = "starter", Name = "Starter", MonthlyPrice = 49, MaxUsers = 10, MaxLeads = 1000, MaxUnits = 200, IsActive = true, CreatedAt = seededAt, UpdatedAt = seededAt },
            new SubscriptionPlan { Id = ProPlanId, Code = "pro", Name = "Pro", MonthlyPrice = 149, MaxUsers = 30, MaxLeads = 10000, MaxUnits = 2000, IsActive = true, CreatedAt = seededAt, UpdatedAt = seededAt },
            new SubscriptionPlan { Id = EnterprisePlanId, Code = "enterprise", Name = "Enterprise", MonthlyPrice = 499, MaxUsers = 1000, MaxLeads = 1000000, MaxUnits = 1000000, IsActive = true, CreatedAt = seededAt, UpdatedAt = seededAt });
    }
}
