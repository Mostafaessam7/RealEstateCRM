using System.Security.Cryptography;
using System.Text;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using RealEstateCRM.Domain.Entities;
using RealEstateCRM.Infrastructure.Persistence;

namespace RealEstateCRM.Infrastructure.Webhooks;

/// <summary>
/// Delivers one webhook attempt: HMAC-SHA256 signs the payload, POSTs it, records a
/// WebhookDelivery row per attempt, and self-schedules the next retry (up to 3 retries after
/// the first attempt — 4 total — with 1m/5m/15m backoff) on failure. A Hangfire job class —
/// must be resolvable via DI.
/// </summary>
public class WebhookDeliveryJob
{
    private static readonly TimeSpan[] RetryDelays = { TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15) };

    private readonly ApplicationDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public WebhookDeliveryJob(ApplicationDbContext db, IHttpClientFactory httpClientFactory, IBackgroundJobClient backgroundJobClient)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _backgroundJobClient = backgroundJobClient;
    }

    public async Task DeliverAsync(Guid subscriptionId, string eventType, string payloadJson, int attemptNumber, CancellationToken cancellationToken)
    {
        var subscription = await _db.WebhookSubscriptions.FirstOrDefaultAsync(s => s.Id == subscriptionId, cancellationToken);
        if (subscription is null || !subscription.IsActive)
        {
            return;
        }

        var success = false;
        int? statusCode = null;
        string? errorMessage = null;

        try
        {
            var client = _httpClientFactory.CreateClient("webhooks");
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, subscription.Url)
            {
                Content = new StringContent(payloadJson, Encoding.UTF8, "application/json")
            };
            httpRequest.Headers.Add("X-Webhook-Event", eventType);
            httpRequest.Headers.Add("X-Webhook-Signature", ComputeSignature(subscription.Secret, payloadJson));
            httpRequest.Headers.Add("X-Webhook-Delivery-Id", Guid.NewGuid().ToString());

            var response = await client.SendAsync(httpRequest, cancellationToken);
            statusCode = (int)response.StatusCode;
            success = response.IsSuccessStatusCode;
            if (!success)
            {
                errorMessage = $"Received HTTP {statusCode}.";
            }
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
        }

        _db.WebhookDeliveries.Add(new WebhookDelivery
        {
            Id = Guid.NewGuid(),
            CompanyId = subscription.CompanyId,
            WebhookSubscriptionId = subscription.Id,
            EventType = eventType,
            Payload = payloadJson,
            AttemptNumber = attemptNumber,
            Success = success,
            ResponseStatusCode = statusCode,
            ErrorMessage = errorMessage,
            CreatedAt = DateTime.UtcNow,
            DeliveredAt = success ? DateTime.UtcNow : null
        });
        await _db.SaveChangesAsync(cancellationToken);

        if (!success && attemptNumber <= RetryDelays.Length)
        {
            var delay = RetryDelays[attemptNumber - 1];
            _backgroundJobClient.Schedule<WebhookDeliveryJob>(
                job => job.DeliverAsync(subscriptionId, eventType, payloadJson, attemptNumber + 1, CancellationToken.None),
                delay);
        }
    }

    public static string ComputeSignature(string secret, string payload)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash);
    }
}
