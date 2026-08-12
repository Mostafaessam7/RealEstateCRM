using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealEstateCRM.Domain.Entities;
using RealEstateCRM.Infrastructure.Identity;

namespace RealEstateCRM.Infrastructure.Persistence.Configurations;

public class CommissionConfiguration : IEntityTypeConfiguration<Commission>
{
    public void Configure(EntityTypeBuilder<Commission> builder)
    {
        builder.ToTable("Commissions");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.CommissionPercentage).HasColumnType("decimal(5,2)");
        builder.Property(c => c.CommissionAmount).HasColumnType("decimal(18,2)");
        builder.Property(c => c.CompanyCommission).HasColumnType("decimal(18,2)");
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(30);

        // One commission per deal.
        builder.HasIndex(c => new { c.CompanyId, c.DealId }).IsUnique();
        builder.HasIndex(c => new { c.CompanyId, c.AgentId });
        builder.HasIndex(c => new { c.CompanyId, c.Status });

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(c => c.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Deal>()
            .WithMany()
            .HasForeignKey(c => c.DealId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(c => c.AgentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
