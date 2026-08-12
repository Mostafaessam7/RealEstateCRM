using RealEstateCRM.Application.Webhooks;

namespace RealEstateCRM.Infrastructure.Webhooks;

/// <summary>
/// Permissive default used when a service is constructed without DI (e.g. directly in unit
/// tests) and no IWebhookPublisher is supplied. Production always resolves the real
/// WebhookPublisher via the DI container.
/// </summary>
public sealed class NullWebhookPublisher : IWebhookPublisher
{
    public static readonly NullWebhookPublisher Instance = new();

    private NullWebhookPublisher()
    {
    }

    public Task PublishAsync(string eventType, object payload, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
