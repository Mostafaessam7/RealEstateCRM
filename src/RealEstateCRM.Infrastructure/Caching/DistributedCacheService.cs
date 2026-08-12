using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using RealEstateCRM.Application.Common.Interfaces;

namespace RealEstateCRM.Infrastructure.Caching;

public class DistributedCacheService : ICacheService
{
    private readonly IDistributedCache _cache;

    public DistributedCacheService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var bytes = await _cache.GetAsync(key, cancellationToken);
        return bytes is null ? default : JsonSerializer.Deserialize<T>(bytes);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value);
        return _cache.SetAsync(key, bytes, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiration }, cancellationToken);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default) =>
        _cache.RemoveAsync(key, cancellationToken);

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan expiration,
        CancellationToken cancellationToken = default)
    {
        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var value = await factory(cancellationToken);
        await SetAsync(key, value, expiration, cancellationToken);
        return value;
    }
}
