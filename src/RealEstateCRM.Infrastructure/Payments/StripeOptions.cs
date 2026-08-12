namespace RealEstateCRM.Infrastructure.Payments;

/// <summary>Bound from configuration ("Stripe" section) / environment — never hardcoded.</summary>
public class StripeOptions
{
    public const string SectionName = "Stripe";

    public string? SecretKey { get; set; }
    public string? WebhookSecret { get; set; }
    public string? PublishableKey { get; set; }
}
