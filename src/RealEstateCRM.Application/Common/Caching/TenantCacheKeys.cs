namespace RealEstateCRM.Application.Common.Caching;

/// <summary>
/// Every key is namespaced by CompanyId per docs/multi-tenancy.md#redis — never share a
/// tenant-specific cache entry across companies. Centralized here so every cache consumer
/// builds keys identically (required for cache invalidation to actually hit).
/// </summary>
public static class TenantCacheKeys
{
    public static string Settings(Guid companyId) => $"tenant:{companyId}:settings";

    public static string Dashboard(Guid companyId) => $"tenant:{companyId}:dashboard";

    public static string AvailableUnits(Guid companyId) => $"tenant:{companyId}:units:available";
}
