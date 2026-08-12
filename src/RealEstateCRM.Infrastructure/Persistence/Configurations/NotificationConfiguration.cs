using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealEstateCRM.Domain.Entities;
using RealEstateCRM.Infrastructure.Identity;

namespace RealEstateCRM.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Type).IsRequired().HasMaxLength(50);
        builder.Property(n => n.Title).IsRequired().HasMaxLength(200);
        builder.Property(n => n.Message).IsRequired().HasMaxLength(2000);

        builder.HasIndex(n => new { n.CompanyId, n.UserId, n.IsRead });

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(n => n.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
