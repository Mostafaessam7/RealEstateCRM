using System.Reflection;
using Microsoft.EntityFrameworkCore;
using RealEstateCRM.Domain.Common;

namespace RealEstateCRM.Infrastructure.Persistence;

/// <summary>
/// Applies a global query filter to every entity implementing <see cref="ITenantEntity"/>
/// and/or <see cref="ISoftDelete"/>, so new entities (Lead, Unit, Deal, ...) get isolation
/// and soft-delete filtering automatically just by inheriting <see cref="TenantEntity"/> /
/// implementing <see cref="ISoftDelete"/> — no per-entity wiring required.
/// </summary>
public static class TenantModelBuilderExtensions
{
    private static readonly MethodInfo SetTenantFilterMethod = GetMethod(nameof(SetTenantFilter));
    private static readonly MethodInfo SetSoftDeleteFilterMethod = GetMethod(nameof(SetSoftDeleteFilter));
    private static readonly MethodInfo SetTenantAndSoftDeleteFilterMethod = GetMethod(nameof(SetTenantAndSoftDeleteFilter));

    private static MethodInfo GetMethod(string name) =>
        typeof(TenantModelBuilderExtensions).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)!;

    /// <param name="context">
    /// Must be passed as the DbContext instance running OnModelCreating (i.e. `this`).
    /// See <see cref="ITenantScopedDbContext"/> for why.
    /// </param>
    public static void ApplyGlobalQueryFilters<TContext>(this ModelBuilder modelBuilder, TContext context)
        where TContext : DbContext, ITenantScopedDbContext
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            var isTenant = typeof(ITenantEntity).IsAssignableFrom(clrType);
            var isSoftDelete = typeof(ISoftDelete).IsAssignableFrom(clrType);

            var method = (isTenant, isSoftDelete) switch
            {
                (true, true) => SetTenantAndSoftDeleteFilterMethod,
                (true, false) => SetTenantFilterMethod,
                (false, true) => SetSoftDeleteFilterMethod,
                _ => null
            };

            method?.MakeGenericMethod(clrType, typeof(TContext)).Invoke(null, new object[] { modelBuilder, context });
        }
    }

    // No resolvable CompanyId (unauthenticated, background job, design-time tooling) => deny by default.

    private static void SetTenantFilter<TEntity, TContext>(ModelBuilder modelBuilder, TContext context)
        where TEntity : class, ITenantEntity
        where TContext : DbContext, ITenantScopedDbContext
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => e.CompanyId == context.CurrentCompanyId);
    }

    private static void SetSoftDeleteFilter<TEntity, TContext>(ModelBuilder modelBuilder, TContext context)
        where TEntity : class, ISoftDelete
        where TContext : DbContext, ITenantScopedDbContext
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => !e.IsDeleted);
    }

    private static void SetTenantAndSoftDeleteFilter<TEntity, TContext>(ModelBuilder modelBuilder, TContext context)
        where TEntity : class, ITenantEntity, ISoftDelete
        where TContext : DbContext, ITenantScopedDbContext
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => e.CompanyId == context.CurrentCompanyId && !e.IsDeleted);
    }
}
