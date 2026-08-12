using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealEstateCRM.Domain.Entities;

namespace RealEstateCRM.Infrastructure.Persistence.Configurations;

public class UnitConfiguration : IEntityTypeConfiguration<Unit>
{
    public void Configure(EntityTypeBuilder<Unit> builder)
    {
        builder.ToTable("Units");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.UnitCode).IsRequired().HasMaxLength(50);
        builder.Property(u => u.PropertyType).HasMaxLength(100);
        builder.Property(u => u.Price).HasColumnType("decimal(18,2)");
        builder.Property(u => u.Area).HasColumnType("decimal(10,2)");
        builder.Property(u => u.Floor).HasMaxLength(30);
        builder.Property(u => u.Location).HasMaxLength(200);
        builder.Property(u => u.DownPayment).HasColumnType("decimal(18,2)");
        builder.Property(u => u.Description).HasMaxLength(2000);
        builder.Property(u => u.Status).HasConversion<string>().HasMaxLength(30);

        // Optimistic concurrency on the existing UpdatedAt column (no schema/migration change —
        // EF just adds it to the UPDATE's WHERE clause). Without this, two agents concurrently
        // reserving the same Available unit both read Status == Available before either writes,
        // and both succeed — a real double-booking race. Now the loser's SaveChangesAsync throws
        // DbUpdateConcurrencyException instead of silently overwriting the winner's reservation.
        builder.Property(u => u.UpdatedAt).IsConcurrencyToken();

        builder.HasIndex(u => new { u.CompanyId, u.ProjectId });
        builder.HasIndex(u => new { u.CompanyId, u.Status });
        builder.HasIndex(u => new { u.CompanyId, u.Price });
        builder.HasIndex(u => new { u.CompanyId, u.PropertyType });

        // UnitCode is unique within its project, per company.
        builder.HasIndex(u => new { u.CompanyId, u.ProjectId, u.UnitCode }).IsUnique();

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(u => u.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(u => u.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
