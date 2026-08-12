using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RealEstateCRM.Application.Webhooks;
using RealEstateCRM.Application.Webhooks.DTOs;
using RealEstateCRM.Domain.Constants;
using RealEstateCRM.Infrastructure.Persistence;
using RealEstateCRM.Infrastructure.Webhooks;
using RealEstateCRM.Tests.MultiTenancy;
using Xunit;

namespace RealEstateCRM.Tests.Webhooks;

public class WebhookTests
{
    private static readonly InMemoryDatabaseRoot Root = new();

    private static ApplicationDbContext CreateDb(string dbName, FakeCurrentTenantService tenant) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(dbName, Root).Options, tenant, new HttpContextAccessor());

    [Fact]
    public async Task CreateAsync_ReturnsSecretOnce_AndPersistsEventTypes()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid(), Roles = { Roles.CompanyAdmin } };
        await using var db = CreateDb(dbName, tenant);

        var service = new WebhookService(db, tenant);
        var created = await service.CreateAsync(new CreateWebhookSubscriptionRequest
        {
            Url = "https://example.com/hooks",
            EventTypes = new List<string> { WebhookEventTypes.LeadCreated, WebhookEventTypes.DealContracted }
        });

        Assert.False(string.IsNullOrWhiteSpace(created.Secret));
        Assert.Contains(WebhookEventTypes.LeadCreated, created.EventTypes);
        Assert.Contains(WebhookEventTypes.DealContracted, created.EventTypes);

        var listed = await service.ListAsync();
        Assert.Single(listed);
    }

    [Fact]
    public async Task PublishAsync_EnqueuesOnlyMatchingActiveSubscriptions()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid(), Roles = { Roles.CompanyAdmin } };
        await using var db = CreateDb(dbName, tenant);

        var webhookService = new WebhookService(db, tenant);
        var matching = await webhookService.CreateAsync(new CreateWebhookSubscriptionRequest
        {
            Url = "https://example.com/leads", EventTypes = new List<string> { WebhookEventTypes.LeadCreated }
        });
        await webhookService.CreateAsync(new CreateWebhookSubscriptionRequest
        {
            Url = "https://example.com/deals", EventTypes = new List<string> { WebhookEventTypes.DealContracted }
        });

        var fakeClient = new FakeBackgroundJobClient();
        var publisher = new WebhookPublisher(db, tenant, fakeClient);

        await publisher.PublishAsync(WebhookEventTypes.LeadCreated, new { fullName = "Test Lead" });

        Assert.Single(fakeClient.CreatedJobs);
        var enqueuedArgs = fakeClient.CreatedJobs[0].Args;
        Assert.Equal(matching.Id, enqueuedArgs[0]);
    }

    [Fact]
    public async Task DeliverAsync_Succeeds_AndSignsThePayload()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid(), Roles = { Roles.CompanyAdmin } };
        await using var db = CreateDb(dbName, tenant);

        var webhookService = new WebhookService(db, tenant);
        var subscription = await webhookService.CreateAsync(new CreateWebhookSubscriptionRequest
        {
            Url = "https://example.com/hooks", EventTypes = new List<string> { WebhookEventTypes.LeadCreated }
        });

        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK);
        var httpClientFactory = new FakeHttpClientFactory(handler);
        var fakeJobClient = new FakeBackgroundJobClient();
        var job = new WebhookDeliveryJob(db, httpClientFactory, fakeJobClient);

        var payload = "{\"eventType\":\"lead.created\"}";
        await job.DeliverAsync(subscription.Id, WebhookEventTypes.LeadCreated, payload, attemptNumber: 1, CancellationToken.None);

        Assert.Equal(payload, handler.LastRequestBody);
        var expectedSignature = WebhookDeliveryJob.ComputeSignature(subscription.Secret, payload);
        Assert.Equal(expectedSignature, handler.LastRequest!.Headers.GetValues("X-Webhook-Signature").Single());

        var deliveries = await webhookService.ListDeliveriesAsync(subscription.Id);
        Assert.Single(deliveries);
        Assert.True(deliveries[0].Success);
        Assert.Equal(200, deliveries[0].ResponseStatusCode);
        Assert.Empty(fakeJobClient.CreatedJobs); // no retry scheduled on success
    }

    [Fact]
    public async Task DeliverAsync_SchedulesRetry_OnFailure()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid(), Roles = { Roles.CompanyAdmin } };
        await using var db = CreateDb(dbName, tenant);

        var webhookService = new WebhookService(db, tenant);
        var subscription = await webhookService.CreateAsync(new CreateWebhookSubscriptionRequest
        {
            Url = "https://example.com/hooks", EventTypes = new List<string> { WebhookEventTypes.LeadCreated }
        });

        var handler = new FakeHttpMessageHandler(HttpStatusCode.InternalServerError);
        var httpClientFactory = new FakeHttpClientFactory(handler);
        var fakeJobClient = new FakeBackgroundJobClient();
        var job = new WebhookDeliveryJob(db, httpClientFactory, fakeJobClient);

        await job.DeliverAsync(subscription.Id, WebhookEventTypes.LeadCreated, "{}", attemptNumber: 1, CancellationToken.None);

        var deliveries = await webhookService.ListDeliveriesAsync(subscription.Id);
        Assert.Single(deliveries);
        Assert.False(deliveries[0].Success);
        Assert.Equal(500, deliveries[0].ResponseStatusCode);
        Assert.Single(fakeJobClient.CreatedJobs); // retry scheduled
    }

    [Fact]
    public async Task DeliverAsync_StopsRetrying_AfterMaxAttempts()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid(), Roles = { Roles.CompanyAdmin } };
        await using var db = CreateDb(dbName, tenant);

        var webhookService = new WebhookService(db, tenant);
        var subscription = await webhookService.CreateAsync(new CreateWebhookSubscriptionRequest
        {
            Url = "https://example.com/hooks", EventTypes = new List<string> { WebhookEventTypes.LeadCreated }
        });

        var handler = new FakeHttpMessageHandler(HttpStatusCode.InternalServerError);
        var httpClientFactory = new FakeHttpClientFactory(handler);
        var fakeJobClient = new FakeBackgroundJobClient();
        var job = new WebhookDeliveryJob(db, httpClientFactory, fakeJobClient);

        await job.DeliverAsync(subscription.Id, WebhookEventTypes.LeadCreated, "{}", attemptNumber: 4, CancellationToken.None);

        Assert.Empty(fakeJobClient.CreatedJobs); // attempt 4 was the last allowed — no further retry
    }
}
