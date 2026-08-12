using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealEstateCRM.Domain.Entities;
using RealEstateCRM.Infrastructure.Identity;

namespace RealEstateCRM.Infrastructure.Persistence.Configurations;

public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("Tasks");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title).IsRequired().HasMaxLength(200);
        builder.Property(t => t.Description).HasMaxLength(2000);
        builder.Property(t => t.Priority).HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(t => new { t.CompanyId, t.AssignedToUserId, t.Status });
        builder.HasIndex(t => new { t.CompanyId, t.DueAt });

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(t => t.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(t => t.AssignedToUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Lead>()
            .WithMany()
            .HasForeignKey(t => t.LeadId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Deal>()
            .WithMany()
            .HasForeignKey(t => t.DealId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
