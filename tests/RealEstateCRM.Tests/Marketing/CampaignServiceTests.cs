using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RealEstateCRM.Application.Common.Exceptions;
using RealEstateCRM.Application.Common.Interfaces;
using RealEstateCRM.Application.Leads.DTOs;
using RealEstateCRM.Application.Marketing.DTOs;
using RealEstateCRM.Domain.Constants;
using RealEstateCRM.Domain.Enums;
using RealEstateCRM.Infrastructure.Email;
using RealEstateCRM.Infrastructure.Leads;
using RealEstateCRM.Infrastructure.Marketing;
using RealEstateCRM.Infrastructure.Persistence;
using RealEstateCRM.Tests.MultiTenancy;
using RealEstateCRM.Tests.WhatsApp;
using Xunit;
using Microsoft.Extensions.Logging.Abstractions;

namespace RealEstateCRM.Tests.Marketing;

public class CampaignServiceTests
{
    private static readonly InMemoryDatabaseRoot Root = new();

    private static ApplicationDbContext CreateDb(string dbName, FakeCurrentTenantService tenant) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(dbName, Root).Options, tenant, new HttpContextAccessor());

    private static IEmailSender EmailSender => new LoggingEmailSender(NullLogger<LoggingEmailSender>.Instance);

    [Fact]
    public async Task SendAsync_SendsOnlyToLeadsMatchingTargetFilters_AndTracksRecipients()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid(), Roles = { Roles.CompanyAdmin } };
        await using var db = CreateDb(dbName, tenant);

        var leadService = new LeadService(db, tenant, new NoOpNotificationService());
        var matching = await leadService.CreateAsync(new CreateLeadRequest { FullName = "Match", Email = "match@test.local", Source = LeadSource.Website });
        await leadService.CreateAsync(new CreateLeadRequest { FullName = "NoMatch", Email = "nomatch@test.local", Source = LeadSource.Referral });
        await leadService.CreateAsync(new CreateLeadRequest { FullName = "NoEmail", Source = LeadSource.Website });

        var service = new CampaignService(db, tenant, EmailSender, new FakeWhatsAppSender());
        var campaign = await service.CreateAsync(new CreateCampaignRequest
        {
            Name = "Website leads blast", Channel = CampaignChannel.Email, Subject = "Hi", Body = "Hello {{FullName}}",
            TargetSource = LeadSource.Website
        });

        var sent = await service.SendAsync(campaign.Id);

        Assert.Equal(CampaignStatus.Sent, sent.Status);
        Assert.Equal(1, sent.RecipientCount);
        Assert.Equal(1, sent.SuccessCount);
        Assert.Equal(0, sent.FailureCount);

        var recipients = await service.ListRecipientsAsync(campaign.Id);
        Assert.Single(recipients);
        Assert.Equal(matching.Id, recipients[0].LeadId);
        Assert.True(recipients[0].Success);
    }

    [Fact]
    public async Task SendAsync_Fails_WhenCampaignAlreadySent()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid(), Roles = { Roles.CompanyAdmin } };
        await using var db = CreateDb(dbName, tenant);

        var service = new CampaignService(db, tenant, EmailSender, new FakeWhatsAppSender());
        var campaign = await service.CreateAsync(new CreateCampaignRequest { Name = "X", Channel = CampaignChannel.Email, Subject = "S", Body = "B" });
        await service.SendAsync(campaign.Id);

        var ex = await Assert.ThrowsAsync<AppException>(() => service.SendAsync(campaign.Id));
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task SendAsync_TracksFailures_WhenWhatsAppSenderRejects()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid(), Roles = { Roles.CompanyAdmin } };
        await using var db = CreateDb(dbName, tenant);

        await new LeadService(db, tenant, new NoOpNotificationService())
            .CreateAsync(new CreateLeadRequest { FullName = "Buyer", Phone = "+201000000000", Source = LeadSource.Website });

        var service = new CampaignService(db, tenant, EmailSender, new FakeWhatsAppSender { Accept = false });
        var campaign = await service.CreateAsync(new CreateCampaignRequest { Name = "WA blast", Channel = CampaignChannel.WhatsApp, Body = "Hi {{FullName}}" });

        var sent = await service.SendAsync(campaign.Id);

        Assert.Equal(1, sent.RecipientCount);
        Assert.Equal(0, sent.SuccessCount);
        Assert.Equal(1, sent.FailureCount);
    }

    [Fact]
    public async Task CreateAsync_Fails_ForSalesAgent()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid(), Roles = { Roles.SalesAgent } };
        await using var db = CreateDb(dbName, tenant);

        var service = new CampaignService(db, tenant, EmailSender, new FakeWhatsAppSender());

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            service.CreateAsync(new CreateCampaignRequest { Name = "X", Channel = CampaignChannel.Email, Subject = "S", Body = "B" }));

        Assert.Equal(403, ex.StatusCode);
    }
}
