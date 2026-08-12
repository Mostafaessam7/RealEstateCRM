using RealEstateCRM.Domain.Enums;

namespace RealEstateCRM.Application.Payments.DTOs;

public class PaymentDto
{
    public Guid Id { get; set; }
    public Guid DealId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "usd";
    public PaymentStatus Status { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateCheckoutRequest
{
    /// <summary>Defaults to the unit's DownPayment when omitted.</summary>
    public decimal? Amount { get; set; }
    public string Currency { get; set; } = "usd";
}

public class CheckoutSessionDto
{
    public Guid PaymentId { get; set; }
    public string CheckoutUrl { get; set; } = string.Empty;
}
