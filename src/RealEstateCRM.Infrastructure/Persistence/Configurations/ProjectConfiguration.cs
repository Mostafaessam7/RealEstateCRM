using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealEstateCRM.Domain.Entities;

namespace RealEstateCRM.Infrastructure.Persistence.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Developer).HasMaxLength(200);
        builder.Property(p => p.Location).HasMaxLength(200);
        builder.Property(p => p.Description).HasMaxLength(2000);
        builder.Property(p => p.StartingPrice).HasColumnType("decimal(18,2)");
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(30);

        builder.HasIndex(p => new { p.CompanyId, p.Name });
        builder.HasIndex(p => new { p.CompanyId, p.Status });

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(p => p.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
