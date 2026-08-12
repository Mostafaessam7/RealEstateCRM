namespace RealEstateCRM.Infrastructure.Persistence;

/// <summary>
/// Implemented by DbContexts that apply tenant query filters. The filter expression must
/// reference this member through the DbContext instance itself (not a captured external
/// service) — EF Core caches the compiled model/filter per context type and specially
/// rebinds DbContext-typed constants in the filter to whichever instance is executing the
/// query. Capturing any other object instead would permanently bake in whichever instance
/// happened to build the model first.
/// </summary>
public interface ITenantScopedDbContext
{
    Guid? CurrentCompanyId { get; }
}
