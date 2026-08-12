namespace RealEstateCRM.Application.Webhooks;

/// <summary>
/// Fire-and-forget event publishing, called from domain services at the moment an important
/// event happens (e.g. a lead is created). Never throws — a webhook delivery failure must
/// never fail the request that triggered it.
/// </summary>
public interface IWebhookPublisher
{
    Task PublishAsync(string eventType, object payload, CancellationToken cancellationToken = default);
}
