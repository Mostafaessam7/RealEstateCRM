using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealEstateCRM.Domain.Entities;
using RealEstateCRM.Infrastructure.Identity;

namespace RealEstateCRM.Infrastructure.Persistence.Configurations;

public class LeadConfiguration : IEntityTypeConfiguration<Lead>
{
    public void Configure(EntityTypeBuilder<Lead> builder)
    {
        builder.ToTable("Leads");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.FullName).IsRequired().HasMaxLength(200);
        builder.Property(l => l.Phone).HasMaxLength(30);
        builder.Property(l => l.Email).HasMaxLength(200);
        builder.Property(l => l.PreferredLocation).HasMaxLength(200);
        builder.Property(l => l.PropertyType).HasMaxLength(100);
        builder.Property(l => l.Notes).HasMaxLength(2000);
        builder.Property(l => l.BudgetMin).HasColumnType("decimal(18,2)");
        builder.Property(l => l.BudgetMax).HasColumnType("decimal(18,2)");
        builder.Property(l => l.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(l => l.Source).HasConversion<string>().HasMaxLength(30);

        builder.HasIndex(l => new { l.CompanyId, l.Status });
        builder.HasIndex(l => new { l.CompanyId, l.AssignedAgentId });
        builder.HasIndex(l => new { l.CompanyId, l.CreatedAt });
        builder.HasIndex(l => new { l.CompanyId, l.Phone });
        builder.HasIndex(l => new { l.CompanyId, l.Source });

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(l => l.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(l => l.AssignedAgentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
