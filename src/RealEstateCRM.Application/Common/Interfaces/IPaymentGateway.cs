namespace RealEstateCRM.Application.Common.Interfaces;

public record CheckoutSession(string SessionId, string CheckoutUrl);

public record WebhookEventResult(bool IsRecognized, string? SessionId, bool Succeeded);

public interface IPaymentGateway
{
    Task<CheckoutSession> CreateCheckoutSessionAsync(
        Guid paymentId, decimal amount, string currency, string successUrl, string cancelUrl, CancellationToken cancellationToken = default);

    /// <summary>Verifies the webhook signature and extracts the outcome. Never throws on a bad signature — returns IsRecognized=false.</summary>
    WebhookEventResult ParseWebhookEvent(string payload, string signatureHeader);
}
