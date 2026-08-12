using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RealEstateCRM.Application.Common.Interfaces;
using RealEstateCRM.Domain.Entities;
using RealEstateCRM.Infrastructure.Identity;

namespace RealEstateCRM.Infrastructure.Persistence;

/// <summary>
/// Automatically audits Create/Update on Leads, Deals, Units, Users, and Commissions per
/// docs/database.md#auditlog — hooked into SaveChanges so nothing can be missed by forgetting
/// to call it from a service. Never captures PasswordHash/SecurityStamp — see docs/CLAUDE.md's
/// "never log passwords" rule, which applies here just as much as to server logs.
/// </summary>
public static class AuditSaveChangesExtensions
{
    private static readonly HashSet<Type> AuditedTypes = new()
    {
        typeof(Lead), typeof(Deal), typeof(Unit), typeof(ApplicationUser), typeof(Commission)
    };

    private static readonly HashSet<string> ExcludedProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "PasswordHash", "SecurityStamp", "ConcurrencyStamp"
    };

    public static void ApplyAuditOnSave(this DbContext context, ICurrentTenantService currentTenant, string? ipAddress)
    {
        var entries = new List<AuditLog>();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (!AuditedTypes.Contains(entry.Entity.GetType()) || entry.State is not (EntityState.Added or EntityState.Modified))
            {
                continue;
            }

            var properties = entry.Properties.Where(p => !ExcludedProperties.Contains(p.Metadata.Name)).ToList();

            var idProperty = properties.FirstOrDefault(p => p.Metadata.Name == "Id");
            if (idProperty?.CurrentValue is not Guid entityId)
            {
                continue;
            }

            var companyIdProperty = properties.FirstOrDefault(p => p.Metadata.Name == "CompanyId");
            var companyId = companyIdProperty?.CurrentValue as Guid? ?? currentTenant.CompanyId;
            if (companyId is null)
            {
                continue; // Can't attribute the change to a tenant — skip rather than fail the whole save.
            }

            string? oldValues = null;
            string? newValues;

            if (entry.State == EntityState.Added)
            {
                newValues = JsonSerializer.Serialize(properties.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue));
            }
            else
            {
                var changed = properties.Where(p => p.IsModified).ToList();
                if (changed.Count == 0)
                {
                    continue;
                }

                oldValues = JsonSerializer.Serialize(changed.ToDictionary(p => p.Metadata.Name, p => p.OriginalValue));
                newValues = JsonSerializer.Serialize(changed.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue));
            }

            entries.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId.Value,
                UserId = currentTenant.UserId,
                Action = entry.State == EntityState.Added ? "Created" : "Updated",
                EntityName = entry.Entity.GetType().Name,
                EntityId = entityId,
                OldValues = oldValues,
                NewValues = newValues,
                IpAddress = ipAddress,
                CreatedAt = DateTime.UtcNow
            });
        }

        if (entries.Count > 0)
        {
            context.Set<AuditLog>().AddRange(entries);
        }
    }
}
