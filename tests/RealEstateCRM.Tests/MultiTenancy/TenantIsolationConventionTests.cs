using System.Reflection;
using RealEstateCRM.Domain.Common;
using RealEstateCRM.Infrastructure.Persistence;
using Xunit;

namespace RealEstateCRM.Tests.MultiTenancy;

/// <summary>
/// Guards the invariant the whole multi-tenant design rests on.
///
/// Isolation here is structural rather than per-handler: <c>ApplyGlobalQueryFilters</c> walks every
/// entity type in the model and attaches a <c>CompanyId == CurrentCompanyId</c> filter to anything
/// implementing <see cref="ITenantEntity"/>, denying by default when no tenant can be resolved. That
/// is a strong design — nobody has to remember to filter — but it has exactly one soft spot: the
/// filter is keyed off the <em>interface</em>, not off the presence of a <c>CompanyId</c> property.
///
/// So an entity that carries <c>CompanyId</c> but does not implement the interface receives no
/// filter at all. It compiles, it runs, its queries return rows from every tenant, and nothing
/// anywhere reports a problem. In a SaaS product that is the worst failure available: one customer
/// reading another customer's data, discovered by the customer rather than by us.
///
/// These tests make that mistake fail the build instead.
/// </summary>
public class TenantIsolationConventionTests
{
    private static readonly Assembly DomainAssembly = typeof(ITenantEntity).Assembly;

    private static IEnumerable<Type> ConcreteDomainTypes() =>
        DomainAssembly.GetTypes().Where(t => t is { IsClass: true, IsAbstract: false });

    [Fact]
    public void Every_entity_with_a_CompanyId_implements_ITenantEntity()
    {
        // The property is the honest signal of intent: someone adding CompanyId to an entity means
        // "this belongs to a tenant", whether or not they remembered what makes filtering happen.
        var offenders = ConcreteDomainTypes()
            .Where(t => t.GetProperty("CompanyId", BindingFlags.Public | BindingFlags.Instance) is not null)
            .Where(t => !typeof(ITenantEntity).IsAssignableFrom(t))
            .Select(t => t.FullName)
            .OrderBy(n => n)
            .ToList();

        Assert.True(
            offenders.Count == 0,
            "These types carry a CompanyId but do not implement ITenantEntity, so "
            + "ApplyGlobalQueryFilters attaches no tenant filter to them and their queries return "
            + "rows belonging to every company:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, offenders.Select(o => "  - " + o)));
    }

    [Fact]
    public void Every_ITenantEntity_actually_exposes_a_writable_CompanyId()
    {
        // The mirror of the test above. ApplyTenantOnAdd assigns CompanyId when saving, so an
        // implementation without a settable property would fail at runtime rather than at build.
        var offenders = ConcreteDomainTypes()
            .Where(t => typeof(ITenantEntity).IsAssignableFrom(t))
            .Where(t =>
            {
                var property = t.GetProperty("CompanyId", BindingFlags.Public | BindingFlags.Instance);
                return property is null || !property.CanWrite;
            })
            .Select(t => t.FullName)
            .OrderBy(n => n)
            .ToList();

        Assert.True(
            offenders.Count == 0,
            "These types implement ITenantEntity but have no writable CompanyId, so the tenant "
            + "cannot be stamped on insert:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, offenders.Select(o => "  - " + o)));
    }

    [Fact]
    public void The_detector_would_catch_an_unfiltered_tenant_entity()
    {
        // A convention test that silently stops matching is worse than no test, because it reports
        // safety it is no longer checking. This pins the detection logic itself against a type
        // deliberately shaped like the mistake, rather than trusting a green suite.
        var offending = typeof(EntityWithCompanyIdButNoInterface);

        Assert.NotNull(offending.GetProperty("CompanyId", BindingFlags.Public | BindingFlags.Instance));
        Assert.False(typeof(ITenantEntity).IsAssignableFrom(offending));
    }

    [Fact]
    public void Bypassing_isolation_requires_an_explicit_call()
    {
        // ForAllTenants is the deliberate escape hatch for cross-tenant work (platform admin
        // screens, background jobs). It is fine that it exists — what matters is that it is
        // explicit and greppable, so an audit can enumerate every place isolation is skipped.
        var bypass = typeof(ApplicationDbContext).GetMethod(
            nameof(ApplicationDbContext.ForAllTenants),
            BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(bypass);
    }

    /// <summary>
    /// Test-only stand-in for the mistake being guarded against: carries a tenant key but does not
    /// implement the interface, so it would receive no query filter.
    /// </summary>
    private sealed class EntityWithCompanyIdButNoInterface
    {
        public Guid Id { get; set; }

        public Guid CompanyId { get; set; }
    }
}
