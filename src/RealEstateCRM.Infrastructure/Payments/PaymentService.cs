using Microsoft.EntityFrameworkCore;
using RealEstateCRM.Application.Common.Exceptions;
using RealEstateCRM.Application.Common.Interfaces;
using RealEstateCRM.Application.Payments;
using RealEstateCRM.Application.Payments.DTOs;
using RealEstateCRM.Domain.Entities;
using RealEstateCRM.Domain.Enums;
using RealEstateCRM.Infrastructure.Persistence;

namespace RealEstateCRM.Infrastructure.Payments;

public class PaymentService : IPaymentService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantService _currentTenant;
    private readonly IPaymentGateway _gateway;

    public PaymentService(ApplicationDbContext db, ICurrentTenantService currentTenant, IPaymentGateway gateway)
    {
        _db = db;
        _currentTenant = currentTenant;
        _gateway = gateway;
    }

    public async Task<CheckoutSessionDto> CreateCheckoutAsync(
        Guid dealId, CreateCheckoutRequest request, string successUrl, string cancelUrl, CancellationToken cancellationToken = default)
    {
        var userId = _currentTenant.UserId ?? throw new AppException("Authenticated user context is required.", 401);

        var deal = await _db.Deals.FirstOrDefaultAsync(d => d.Id == dealId, cancellationToken)
            ?? throw new AppException("Deal not found.", 404);

        var amount = request.Amount;
        if (!amount.HasValue)
        {
            var unit = await _db.Units.AsNoTracking().FirstOrDefaultAsync(u => u.Id == deal.UnitId, cancellationToken);
            amount = unit?.DownPayment ?? throw new AppException("No amount given and the unit has no down payment configured.", 400);
        }

        if (amount <= 0)
        {
            throw new AppException("Amount must be greater than zero.", 400);
        }

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            DealId = dealId,
            Amount = amount.Value,
            Currency = request.Currency,
            Status = PaymentStatus.Pending,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync(cancellationToken);

        var session = await _gateway.CreateCheckoutSessionAsync(payment.Id, amount.Value, request.Currency, successUrl, cancelUrl, cancellationToken);

        payment.GatewayCheckoutSessionId = session.SessionId;
        payment.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return new CheckoutSessionDto { PaymentId = payment.Id, CheckoutUrl = session.CheckoutUrl };
    }

    public async Task<IReadOnlyList<PaymentDto>> ListForDealAsync(Guid dealId, CancellationToken cancellationToken = default)
    {
        var payments = await _db.Payments.AsNoTracking()
            .Where(p => p.DealId == dealId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        return payments.Select(ToDto).ToList();
    }

    public async Task HandleWebhookAsync(string payload, string signatureHeader, CancellationToken cancellationToken = default)
    {
        var result = _gateway.ParseWebhookEvent(payload, signatureHeader);
        if (!result.IsRecognized || result.SessionId is null)
        {
            return;
        }

        // Unauthenticated webhook call — no tenant context. The Payment already carries its
        // own CompanyId from creation; look it up across all tenants by the gateway session id.
        var payment = await _db.ForAllTenants<Payment>()
            .FirstOrDefaultAsync(p => p.GatewayCheckoutSessionId == result.SessionId, cancellationToken);

        if (payment is null || payment.Status != PaymentStatus.Pending)
        {
            return;
        }

        payment.Status = result.Succeeded ? PaymentStatus.Paid : PaymentStatus.Failed;
        payment.PaidAt = result.Succeeded ? DateTime.UtcNow : null;
        payment.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static PaymentDto ToDto(Payment payment) => new()
    {
        Id = payment.Id,
        DealId = payment.DealId,
        Amount = payment.Amount,
        Currency = payment.Currency,
        Status = payment.Status,
        PaidAt = payment.PaidAt,
        CreatedAt = payment.CreatedAt
    };
}
