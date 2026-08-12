using Microsoft.Extensions.Logging;
using RealEstateCRM.Application.Common.Interfaces;

namespace RealEstateCRM.Infrastructure.Payments;

/// <summary>
/// Placeholder IPaymentGateway used when Stripe:SecretKey is not configured — mirrors
/// LoggingEmailSender/LoggingWhatsAppSender. Logs instead of creating a real checkout session
/// so the request/response shape is wired end-to-end without a real Stripe account.
/// </summary>
public class NoOpPaymentGateway : IPaymentGateway
{
    private readonly ILogger<NoOpPaymentGateway> _logger;

    public NoOpPaymentGateway(ILogger<NoOpPaymentGateway> logger)
    {
        _logger = logger;
    }

    public Task<CheckoutSession> CreateCheckoutSessionAsync(
        Guid paymentId, decimal amount, string currency, string successUrl, string cancelUrl, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "Stripe is not configured (Stripe:SecretKey missing). Would create a {Amount} {Currency} checkout session for payment {PaymentId}.",
            amount, currency, paymentId);

        return Task.FromResult(new CheckoutSession($"noop_{paymentId}", cancelUrl));
    }

    public WebhookEventResult ParseWebhookEvent(string payload, string signatureHeader) => new(false, null, false);
}
