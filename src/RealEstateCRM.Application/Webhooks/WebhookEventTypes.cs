namespace RealEstateCRM.Application.Webhooks;

/// <summary>The set of event types a WebhookSubscription can subscribe to.</summary>
public static class WebhookEventTypes
{
    public const string LeadCreated = "lead.created";
    public const string LeadStatusChanged = "lead.status_changed";
    public const string DealContracted = "deal.contracted";

    public static readonly string[] All = { LeadCreated, LeadStatusChanged, DealContracted };
}
