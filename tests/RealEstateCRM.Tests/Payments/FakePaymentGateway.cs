using RealEstateCRM.Application.Common.Interfaces;

namespace RealEstateCRM.Tests.Payments;

internal class FakePaymentGateway : IPaymentGateway
{
    public bool WebhookSucceeded { get; set; } = true;
    public string? LastSessionId { get; private set; }

    public Task<CheckoutSession> CreateCheckoutSessionAsync(
        Guid paymentId, decimal amount, string currency, string successUrl, string cancelUrl, CancellationToken cancellationToken = default)
    {
        LastSessionId = $"cs_test_{paymentId}";
        return Task.FromResult(new CheckoutSession(LastSessionId, $"https://checkout.stripe.com/{LastSessionId}"));
    }

    public WebhookEventResult ParseWebhookEvent(string payload, string signatureHeader) =>
        new(true, LastSessionId, WebhookSucceeded);
}
