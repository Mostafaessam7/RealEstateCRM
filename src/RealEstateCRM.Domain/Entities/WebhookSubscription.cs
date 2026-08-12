using RealEstateCRM.Domain.Common;

namespace RealEstateCRM.Domain.Entities;

/// <summary>
/// A company-owned endpoint that receives POSTed event payloads, signed with Secret via
/// HMAC-SHA256 (header X-Webhook-Signature). Secret is shown once, at creation.
/// </summary>
public class WebhookSubscription : TenantEntity
{
    public string Url { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;

    /// <summary>Comma-separated event type keys, e.g. "lead.created,deal.contracted".</summary>
    public string EventTypes { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public Guid CreatedByUserId { get; set; }
}
