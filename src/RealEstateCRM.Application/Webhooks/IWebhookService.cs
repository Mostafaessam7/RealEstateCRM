using RealEstateCRM.Application.Webhooks.DTOs;

namespace RealEstateCRM.Application.Webhooks;

/// <summary>Management (CRUD + delivery history) for the current company's webhook subscriptions.</summary>
public interface IWebhookService
{
    Task<IReadOnlyList<WebhookSubscriptionDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<CreatedWebhookSubscriptionDto> CreateAsync(CreateWebhookSubscriptionRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WebhookDeliveryDto>> ListDeliveriesAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
}
