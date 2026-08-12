using Microsoft.EntityFrameworkCore;
using RealEstateCRM.Application.Common.Exceptions;
using RealEstateCRM.Application.Common.Interfaces;
using RealEstateCRM.Domain.Common;

namespace RealEstateCRM.Infrastructure.Persistence;

/// <summary>
/// Forces CompanyId on newly-added tenant-owned entities from the trusted authenticated
/// context, overwriting whatever the caller set. Never trust CompanyId from a request body.
/// </summary>
public static class TenantSaveChangesExtensions
{
    public static void ApplyTenantOnAdd(this DbContext context, ICurrentTenantService currentTenant)
    {
        foreach (var entry in context.ChangeTracker.Entries<ITenantEntity>())
        {
            if (entry.State != EntityState.Added)
            {
                continue;
            }

            entry.Entity.CompanyId = currentTenant.CompanyId
                ?? throw new AppException("Cannot create a tenant-owned record without an authenticated company context.", 401);
        }
    }
}
