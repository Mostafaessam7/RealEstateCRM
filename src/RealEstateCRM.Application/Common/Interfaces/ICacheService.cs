namespace RealEstateCRM.Application.Common.Interfaces;

/// <summary>
/// Thin wrapper over IDistributedCache (Redis). Callers are responsible for using
/// tenant-scoped keys — see TenantCacheKeys — never a shared/global key for tenant data.
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default);

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Cache-aside: returns the cached value, or calls <paramref name="factory"/>, caches, and returns it.</summary>
    Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan expiration,
        CancellationToken cancellationToken = default);
}
