using RealEstateCRM.Domain.Common;
using RealEstateCRM.Domain.Enums;

namespace RealEstateCRM.Domain.Entities;

/// <summary>An online payment (e.g. a deal's down payment) collected via a payment gateway (Stripe).</summary>
public class Payment : TenantEntity
{
    public Guid DealId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "usd";
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? GatewayCheckoutSessionId { get; set; }
    public string? GatewayPaymentIntentId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime? PaidAt { get; set; }
}
