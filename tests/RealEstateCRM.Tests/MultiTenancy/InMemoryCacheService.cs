using RealEstateCRM.Application.Common.Interfaces;

namespace RealEstateCRM.Tests.MultiTenancy;

/// <summary>Real in-process cache-aside behavior (including invalidation) without needing Redis.</summary>
internal class InMemoryCacheService : ICacheService
{
    private readonly Dictionary<string, object?> _store = new();

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.TryGetValue(key, out var value) ? (T?)value : default);

    public Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        _store[key] = value;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _store.Remove(key);
        return Task.CompletedTask;
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan expiration,
        CancellationToken cancellationToken = default)
    {
        if (_store.TryGetValue(key, out var cached) && cached is not null)
        {
            return (T)cached;
        }

        var value = await factory(cancellationToken);
        _store[key] = value;
        return value;
    }
}
