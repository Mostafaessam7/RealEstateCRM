using RealEstateCRM.Application.Payments.DTOs;

namespace RealEstateCRM.Application.Payments;

public interface IPaymentService
{
    /// <summary>Creates a Pending Payment and a gateway Checkout session for a Deal's down payment.</summary>
    Task<CheckoutSessionDto> CreateCheckoutAsync(Guid dealId, CreateCheckoutRequest request, string successUrl, string cancelUrl, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PaymentDto>> ListForDealAsync(Guid dealId, CancellationToken cancellationToken = default);

    /// <summary>Verifies the gateway webhook signature and updates the matching Payment's status. Never throws.</summary>
    Task HandleWebhookAsync(string payload, string signatureHeader, CancellationToken cancellationToken = default);
}
