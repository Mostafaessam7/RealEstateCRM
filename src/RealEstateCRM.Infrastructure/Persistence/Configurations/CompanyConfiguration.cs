using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealEstateCRM.Domain.Entities;

namespace RealEstateCRM.Infrastructure.Persistence.Configurations;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("Companies");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Slug)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(c => c.Slug)
            .IsUnique();

        builder.Property(c => c.Phone).HasMaxLength(50);
        builder.Property(c => c.Email).HasMaxLength(200);
    }
}
