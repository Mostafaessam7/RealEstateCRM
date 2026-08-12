using System.Text.Json;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using RealEstateCRM.Application.Common.Interfaces;
using RealEstateCRM.Application.Webhooks;
using RealEstateCRM.Infrastructure.Persistence;

namespace RealEstateCRM.Infrastructure.Webhooks;

public class WebhookPublisher : IWebhookPublisher
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantService _currentTenant;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public WebhookPublisher(ApplicationDbContext db, ICurrentTenantService currentTenant, IBackgroundJobClient backgroundJobClient)
    {
        _db = db;
        _currentTenant = currentTenant;
        _backgroundJobClient = backgroundJobClient;
    }

    public async Task PublishAsync(string eventType, object payload, CancellationToken cancellationToken = default)
    {
        var companyId = _currentTenant.CompanyId;
        if (companyId is null)
        {
            return;
        }

        var subscriptions = await _db.WebhookSubscriptions.AsNoTracking()
            .Where(s => s.CompanyId == companyId.Value && s.IsActive)
            .ToListAsync(cancellationToken);

        if (subscriptions.Count == 0)
        {
            return;
        }

        var payloadJson = JsonSerializer.Serialize(new { eventType, occurredAt = DateTime.UtcNow, data = payload });

        foreach (var subscription in subscriptions)
        {
            if (!MatchesEventType(subscription.EventTypes, eventType))
            {
                continue;
            }

            _backgroundJobClient.Enqueue<WebhookDeliveryJob>(
                job => job.DeliverAsync(subscription.Id, eventType, payloadJson, 1, CancellationToken.None));
        }
    }

    private static bool MatchesEventType(string commaSeparatedEventTypes, string eventType) =>
        commaSeparatedEventTypes.Split(',', StringSplitOptions.RemoveEmptyEntries).Contains(eventType);
}
