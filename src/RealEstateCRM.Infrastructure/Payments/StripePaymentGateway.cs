using Microsoft.Extensions.Options;
using RealEstateCRM.Application.Common.Interfaces;
using Stripe;
using Stripe.Checkout;

namespace RealEstateCRM.Infrastructure.Payments;

/// <summary>Real Stripe Checkout integration. Only registered when Stripe:SecretKey is configured — see DependencyInjection.</summary>
public class StripePaymentGateway : IPaymentGateway
{
    private readonly StripeOptions _options;
    private readonly SessionService _sessionService;

    public StripePaymentGateway(IOptions<StripeOptions> options)
    {
        _options = options.Value;
        var client = new StripeClient(_options.SecretKey);
        _sessionService = new SessionService(client);
    }

    public async Task<CheckoutSession> CreateCheckoutSessionAsync(
        Guid paymentId, decimal amount, string currency, string successUrl, string cancelUrl, CancellationToken cancellationToken = default)
    {
        var session = await _sessionService.CreateAsync(new SessionCreateOptions
        {
            Mode = "payment",
            PaymentMethodTypes = new List<string> { "card" },
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            Metadata = new Dictionary<string, string> { ["paymentId"] = paymentId.ToString() },
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = currency,
                        UnitAmount = (long)Math.Round(amount * 100, MidpointRounding.AwayFromZero),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = "Real Estate CRM — deal down payment"
                        }
                    }
                }
            }
        }, cancellationToken: cancellationToken);

        return new CheckoutSession(session.Id, session.Url);
    }

    public WebhookEventResult ParseWebhookEvent(string payload, string signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(_options.WebhookSecret))
        {
            return new WebhookEventResult(false, null, false);
        }

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(payload, signatureHeader, _options.WebhookSecret);

            if (stripeEvent.Data.Object is Session session)
            {
                var succeeded = stripeEvent.Type == "checkout.session.completed" && session.PaymentStatus == "paid";
                return new WebhookEventResult(true, session.Id, succeeded);
            }

            return new WebhookEventResult(false, null, false);
        }
        catch (StripeException)
        {
            return new WebhookEventResult(false, null, false);
        }
    }
}
