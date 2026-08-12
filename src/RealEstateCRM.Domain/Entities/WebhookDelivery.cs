using RealEstateCRM.Domain.Common;

namespace RealEstateCRM.Domain.Entities;

/// <summary>One delivery attempt's outcome for a WebhookSubscription. Immutable audit trail.</summary>
public class WebhookDelivery : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid WebhookSubscriptionId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public int AttemptNumber { get; set; } = 1;
    public bool Success { get; set; }
    public int? ResponseStatusCode { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
}
