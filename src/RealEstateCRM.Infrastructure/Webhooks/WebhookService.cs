using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RealEstateCRM.Application.Common.Exceptions;
using RealEstateCRM.Application.Common.Interfaces;
using RealEstateCRM.Application.Webhooks;
using RealEstateCRM.Application.Webhooks.DTOs;
using RealEstateCRM.Domain.Entities;
using RealEstateCRM.Infrastructure.Persistence;

namespace RealEstateCRM.Infrastructure.Webhooks;

public class WebhookService : IWebhookService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantService _currentTenant;

    public WebhookService(ApplicationDbContext db, ICurrentTenantService currentTenant)
    {
        _db = db;
        _currentTenant = currentTenant;
    }

    public async Task<IReadOnlyList<WebhookSubscriptionDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var subscriptions = await _db.WebhookSubscriptions.AsNoTracking()
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync(cancellationToken);

        return subscriptions.Select(ToDto).ToList();
    }

    public async Task<CreatedWebhookSubscriptionDto> CreateAsync(CreateWebhookSubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        var userId = _currentTenant.UserId ?? throw new AppException("Authenticated user context is required.", 401);

        var secret = GenerateSecret();

        var subscription = new WebhookSubscription
        {
            Id = Guid.NewGuid(),
            Url = request.Url,
            Secret = secret,
            EventTypes = string.Join(",", request.EventTypes),
            IsActive = true,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.WebhookSubscriptions.Add(subscription);
        await _db.SaveChangesAsync(cancellationToken);

        var dto = ToDto(subscription);
        return new CreatedWebhookSubscriptionDto
        {
            Id = dto.Id,
            Url = dto.Url,
            EventTypes = dto.EventTypes,
            IsActive = dto.IsActive,
            CreatedAt = dto.CreatedAt,
            Secret = secret
        };
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var subscription = await _db.WebhookSubscriptions.FirstOrDefaultAsync(w => w.Id == id, cancellationToken)
            ?? throw new AppException("Webhook subscription not found.", 404);

        _db.WebhookSubscriptions.Remove(subscription);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WebhookDeliveryDto>> ListDeliveriesAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        var deliveries = await _db.WebhookDeliveries.AsNoTracking()
            .Where(d => d.WebhookSubscriptionId == subscriptionId)
            .OrderByDescending(d => d.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        return deliveries.Select(d => new WebhookDeliveryDto
        {
            Id = d.Id,
            EventType = d.EventType,
            AttemptNumber = d.AttemptNumber,
            Success = d.Success,
            ResponseStatusCode = d.ResponseStatusCode,
            ErrorMessage = d.ErrorMessage,
            CreatedAt = d.CreatedAt,
            DeliveredAt = d.DeliveredAt
        }).ToList();
    }

    private static string GenerateSecret() => Convert.ToHexString(RandomNumberGenerator.GetBytes(24));

    private static WebhookSubscriptionDto ToDto(WebhookSubscription subscription) => new()
    {
        Id = subscription.Id,
        Url = subscription.Url,
        EventTypes = subscription.EventTypes.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
        IsActive = subscription.IsActive,
        CreatedAt = subscription.CreatedAt
    };
}
