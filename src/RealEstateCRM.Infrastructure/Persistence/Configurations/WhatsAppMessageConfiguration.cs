using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RealEstateCRM.Domain.Entities;
using RealEstateCRM.Infrastructure.Identity;

namespace RealEstateCRM.Infrastructure.Persistence.Configurations;

public class WhatsAppMessageConfiguration : IEntityTypeConfiguration<WhatsAppMessage>
{
    public void Configure(EntityTypeBuilder<WhatsAppMessage> builder)
    {
        builder.ToTable("WhatsAppMessages");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.ToPhone).HasMaxLength(30).IsRequired();
        builder.Property(m => m.Body).HasMaxLength(2000).IsRequired();
        builder.Property(m => m.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.ErrorMessage).HasMaxLength(500);

        builder.HasIndex(m => new { m.CompanyId, m.LeadId });

        builder.HasOne<Lead>()
            .WithMany()
            .HasForeignKey(m => m.LeadId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(m => m.SentByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<WhatsAppTemplate>()
            .WithMany()
            .HasForeignKey(m => m.TemplateId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
