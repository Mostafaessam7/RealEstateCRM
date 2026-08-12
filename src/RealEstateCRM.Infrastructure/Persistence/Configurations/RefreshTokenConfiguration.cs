using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealEstateCRM.Domain.Entities;
using RealEstateCRM.Infrastructure.Identity;

namespace RealEstateCRM.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.TokenHash)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(rt => rt.TokenHash).IsUnique();
        builder.HasIndex(rt => rt.UserId);

        builder.Property(rt => rt.CreatedByIp).HasMaxLength(64);
        builder.Property(rt => rt.RevokedByIp).HasMaxLength(64);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(rt => rt.IsActive);
    }
}
