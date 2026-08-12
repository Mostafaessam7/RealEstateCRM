using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealEstateCRM.Domain.Entities;
using RealEstateCRM.Infrastructure.Identity;

namespace RealEstateCRM.Infrastructure.Persistence.Configurations;

public class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.ToTable("ApiKeys");
        builder.HasKey(k => k.Id);

        builder.Property(k => k.Name).HasMaxLength(150).IsRequired();
        builder.Property(k => k.KeyPrefix).HasMaxLength(20).IsRequired();
        builder.Property(k => k.HashedKey).HasMaxLength(128).IsRequired();
        builder.Property(k => k.Scopes).HasMaxLength(50).IsRequired();

        builder.HasIndex(k => k.HashedKey).IsUnique();
        builder.HasIndex(k => new { k.CompanyId, k.IsActive });

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(k => k.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
