namespace RealEstateCRM.Application.Webhooks.DTOs;

public class WebhookSubscriptionDto
{
    public Guid Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public List<string> EventTypes { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Returned only once, at creation — the signing secret is never retrievable again.</summary>
public class CreatedWebhookSubscriptionDto : WebhookSubscriptionDto
{
    public string Secret { get; set; } = string.Empty;
}

public class CreateWebhookSubscriptionRequest
{
    public string Url { get; set; } = string.Empty;
    public List<string> EventTypes { get; set; } = new();
}

public class WebhookDeliveryDto
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public int AttemptNumber { get; set; }
    public bool Success { get; set; }
    public int? ResponseStatusCode { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
}
