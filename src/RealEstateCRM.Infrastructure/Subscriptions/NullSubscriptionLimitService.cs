using RealEstateCRM.Application.Subscriptions;

namespace RealEstateCRM.Infrastructure.Subscriptions;

/// <summary>
/// Permissive default used when a service is constructed without DI (e.g. directly in unit
/// tests) and no ISubscriptionLimitService is supplied. Production always resolves the real
/// SubscriptionLimitService via the DI container.
/// </summary>
public sealed class NullSubscriptionLimitService : ISubscriptionLimitService
{
    public static readonly NullSubscriptionLimitService Instance = new();

    private NullSubscriptionLimitService()
    {
    }

    public Task EnsureCanAddUserAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task EnsureCanAddLeadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task EnsureCanAddUnitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
