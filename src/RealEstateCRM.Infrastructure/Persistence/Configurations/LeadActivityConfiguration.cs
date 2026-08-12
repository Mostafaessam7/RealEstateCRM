using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealEstateCRM.Domain.Entities;
using RealEstateCRM.Infrastructure.Identity;

namespace RealEstateCRM.Infrastructure.Persistence.Configurations;

public class LeadActivityConfiguration : IEntityTypeConfiguration<LeadActivity>
{
    public void Configure(EntityTypeBuilder<LeadActivity> builder)
    {
        builder.ToTable("LeadActivities");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Type).HasConversion<string>().HasMaxLength(30);
        builder.Property(a => a.Description).HasMaxLength(2000);

        builder.HasIndex(a => new { a.CompanyId, a.LeadId });
        builder.HasIndex(a => new { a.CompanyId, a.ActivityDate });

        builder.HasOne<Lead>()
            .WithMany()
            .HasForeignKey(a => a.LeadId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
