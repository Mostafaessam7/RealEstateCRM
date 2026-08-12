using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RealEstateCRM.Application.Common.Exceptions;
using RealEstateCRM.Application.Common.Interfaces;
using RealEstateCRM.Application.Leads.DTOs;
using RealEstateCRM.Application.WhatsApp.DTOs;
using RealEstateCRM.Domain.Constants;
using RealEstateCRM.Domain.Enums;
using RealEstateCRM.Infrastructure.Leads;
using RealEstateCRM.Infrastructure.Persistence;
using RealEstateCRM.Infrastructure.WhatsApp;
using RealEstateCRM.Tests.MultiTenancy;
using Xunit;

namespace RealEstateCRM.Tests.WhatsApp;

internal class FakeWhatsAppSender : IWhatsAppSender
{
    public bool Accept { get; set; } = true;
    public Task<bool> SendAsync(string toPhone, string body, CancellationToken cancellationToken = default) => Task.FromResult(Accept);
}

public class WhatsAppServiceTests
{
    private static readonly InMemoryDatabaseRoot Root = new();

    private static ApplicationDbContext CreateDb(string dbName, FakeCurrentTenantService tenant) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(dbName, Root).Options, tenant, new HttpContextAccessor());

    [Fact]
    public async Task SendToLeadAsync_WithRawBody_LogsSentMessage()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid(), Roles = { Roles.SalesAgent } };
        await using var db = CreateDb(dbName, tenant);

        var lead = await new LeadService(db, tenant, new NoOpNotificationService())
            .CreateAsync(new CreateLeadRequest { FullName = "Buyer", Phone = "+201000000000", Source = LeadSource.Website });

        var service = new WhatsAppService(db, tenant, new FakeWhatsAppSender());
        var message = await service.SendToLeadAsync(lead.Id, new SendWhatsAppRequest { Body = "Hello there" });

        Assert.Equal(WhatsAppMessageStatus.Sent, message.Status);
        Assert.Equal("+201000000000", message.ToPhone);
        Assert.NotNull(message.SentAt);
    }

    [Fact]
    public async Task SendToLeadAsync_WithTemplate_RendersPlaceholders()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid(), Roles = { Roles.CompanyAdmin } };
        await using var db = CreateDb(dbName, tenant);

        var lead = await new LeadService(db, tenant, new NoOpNotificationService())
            .CreateAsync(new CreateLeadRequest { FullName = "Sara", Phone = "+201111111111", Source = LeadSource.Website, PreferredLocation = "New Cairo" });

        var service = new WhatsAppService(db, tenant, new FakeWhatsAppSender());
        var template = await service.CreateTemplateAsync(new CreateWhatsAppTemplateRequest
        {
            Name = "Welcome",
            Body = "Hi {{FullName}}, we have new units in {{PreferredLocation}}!"
        });

        var message = await service.SendToLeadAsync(lead.Id, new SendWhatsAppRequest { TemplateId = template.Id });

        Assert.Equal("Hi Sara, we have new units in New Cairo!", message.Body);
    }

    [Fact]
    public async Task SendToLeadAsync_Fails_WhenLeadHasNoPhone()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid(), Roles = { Roles.SalesAgent } };
        await using var db = CreateDb(dbName, tenant);

        var lead = await new LeadService(db, tenant, new NoOpNotificationService())
            .CreateAsync(new CreateLeadRequest { FullName = "No Phone", Source = LeadSource.Website });

        var service = new WhatsAppService(db, tenant, new FakeWhatsAppSender());

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            service.SendToLeadAsync(lead.Id, new SendWhatsAppRequest { Body = "Hi" }));

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task SendToLeadAsync_MarksFailed_WhenSenderRejects()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid(), Roles = { Roles.SalesAgent } };
        await using var db = CreateDb(dbName, tenant);

        var lead = await new LeadService(db, tenant, new NoOpNotificationService())
            .CreateAsync(new CreateLeadRequest { FullName = "Buyer", Phone = "+201000000000", Source = LeadSource.Website });

        var service = new WhatsAppService(db, tenant, new FakeWhatsAppSender { Accept = false });
        var message = await service.SendToLeadAsync(lead.Id, new SendWhatsAppRequest { Body = "Hi" });

        Assert.Equal(WhatsAppMessageStatus.Failed, message.Status);
        Assert.NotNull(message.ErrorMessage);
    }

    [Fact]
    public async Task CreateTemplateAsync_Fails_ForSalesAgent()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid(), Roles = { Roles.SalesAgent } };
        await using var db = CreateDb(dbName, tenant);

        var service = new WhatsAppService(db, tenant, new FakeWhatsAppSender());

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            service.CreateTemplateAsync(new CreateWhatsAppTemplateRequest { Name = "X", Body = "Y" }));

        Assert.Equal(403, ex.StatusCode);
    }

    [Fact]
    public async Task ListMessagesAsync_ReturnsMostRecentFirst()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid(), Roles = { Roles.SalesAgent } };
        await using var db = CreateDb(dbName, tenant);

        var lead = await new LeadService(db, tenant, new NoOpNotificationService())
            .CreateAsync(new CreateLeadRequest { FullName = "Buyer", Phone = "+201000000000", Source = LeadSource.Website });

        var service = new WhatsAppService(db, tenant, new FakeWhatsAppSender());
        await service.SendToLeadAsync(lead.Id, new SendWhatsAppRequest { Body = "First" });
        await service.SendToLeadAsync(lead.Id, new SendWhatsAppRequest { Body = "Second" });

        var messages = await service.ListMessagesAsync(lead.Id);

        Assert.Equal(2, messages.Count);
        Assert.Equal("Second", messages[0].Body);
    }
}
