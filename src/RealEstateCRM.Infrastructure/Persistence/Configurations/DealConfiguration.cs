using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealEstateCRM.Domain.Entities;
using RealEstateCRM.Infrastructure.Identity;

namespace RealEstateCRM.Infrastructure.Persistence.Configurations;

public class DealConfiguration : IEntityTypeConfiguration<Deal>
{
    public void Configure(EntityTypeBuilder<Deal> builder)
    {
        builder.ToTable("Deals");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.DealValue).HasColumnType("decimal(18,2)");
        builder.Property(d => d.Notes).HasMaxLength(2000);
        builder.Property(d => d.Status).HasConversion<string>().HasMaxLength(30);

        builder.HasIndex(d => new { d.CompanyId, d.Status });
        builder.HasIndex(d => new { d.CompanyId, d.SalesAgentId });
        builder.HasIndex(d => new { d.CompanyId, d.CreatedAt });

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(d => d.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Lead>()
            .WithMany()
            .HasForeignKey(d => d.LeadId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Unit>()
            .WithMany()
            .HasForeignKey(d => d.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(d => d.SalesAgentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
