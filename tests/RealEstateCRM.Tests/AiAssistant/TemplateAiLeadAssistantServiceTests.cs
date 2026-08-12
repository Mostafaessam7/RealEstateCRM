using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RealEstateCRM.Application.Common.Exceptions;
using RealEstateCRM.Application.Leads.DTOs;
using RealEstateCRM.Domain.Constants;
using RealEstateCRM.Domain.Enums;
using RealEstateCRM.Infrastructure.AiAssistant;
using RealEstateCRM.Infrastructure.Leads;
using RealEstateCRM.Infrastructure.Persistence;
using RealEstateCRM.Tests.MultiTenancy;
using Xunit;

namespace RealEstateCRM.Tests.AiAssistant;

public class TemplateAiLeadAssistantServiceTests
{
    private static readonly InMemoryDatabaseRoot Root = new();

    private static ApplicationDbContext CreateDb(string dbName, FakeCurrentTenantService tenant) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(dbName, Root).Options, tenant, new HttpContextAccessor());

    [Fact]
    public async Task GetInsightAsync_IncludesLeadDetails_InSummaryAndMessage()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid(), Roles = { Roles.CompanyAdmin } };
        await using var db = CreateDb(dbName, tenant);

        var lead = await new LeadService(db, tenant, new NoOpNotificationService()).CreateAsync(new CreateLeadRequest
        {
            FullName = "Nour", Source = LeadSource.Website,
            BudgetMin = 1_000_000, BudgetMax = 1_500_000,
            PreferredLocation = "6th of October", PropertyType = "Villa"
        });

        var service = new TemplateAiLeadAssistantService(db);
        var insight = await service.GetInsightAsync(lead.Id);

        Assert.Contains("Nour", insight.Summary);
        Assert.Contains("New", insight.Summary);
        Assert.Contains("Villa", insight.SuggestedFollowUpMessage);
        Assert.Contains("6th of October", insight.SuggestedFollowUpMessage);
        Assert.False(string.IsNullOrWhiteSpace(insight.NextBestAction));
    }

    [Fact]
    public async Task GetInsightAsync_SuggestsQualifyingAction_ForNewLead()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid(), Roles = { Roles.CompanyAdmin } };
        await using var db = CreateDb(dbName, tenant);

        var lead = await new LeadService(db, tenant, new NoOpNotificationService())
            .CreateAsync(new CreateLeadRequest { FullName = "Omar", Source = LeadSource.Referral });

        var service = new TemplateAiLeadAssistantService(db);
        var insight = await service.GetInsightAsync(lead.Id);

        Assert.Contains("24 hours", insight.NextBestAction);
    }

    [Fact]
    public async Task GetInsightAsync_Fails_WhenLeadNotFound()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid(), Roles = { Roles.CompanyAdmin } };
        await using var db = CreateDb(dbName, tenant);

        var service = new TemplateAiLeadAssistantService(db);

        var ex = await Assert.ThrowsAsync<AppException>(() => service.GetInsightAsync(Guid.NewGuid()));
        Assert.Equal(404, ex.StatusCode);
    }
}
