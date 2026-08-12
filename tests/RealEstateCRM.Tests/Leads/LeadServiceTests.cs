using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RealEstateCRM.Application.Common.Exceptions;
using RealEstateCRM.Application.Leads.DTOs;
using RealEstateCRM.Domain.Enums;
using RealEstateCRM.Infrastructure.Identity;
using RealEstateCRM.Infrastructure.Leads;
using RealEstateCRM.Infrastructure.Persistence;
using RealEstateCRM.Tests.MultiTenancy;
using Xunit;

namespace RealEstateCRM.Tests.Leads;

public class LeadServiceTests
{
    private static readonly InMemoryDatabaseRoot Root = new();

    private static ApplicationDbContext CreateDb(string dbName, FakeCurrentTenantService tenant) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(dbName, Root).Options, tenant, new HttpContextAccessor());

    private static async Task<Guid> SeedAgentAsync(ApplicationDbContext db, Guid companyId)
    {
        var agentId = Guid.NewGuid();
        db.Users.Add(new ApplicationUser
        {
            Id = agentId,
            CompanyId = companyId,
            FullName = "Test Agent",
            Email = $"{agentId}@test.local",
            NormalizedEmail = $"{agentId}@test.local".ToUpperInvariant(),
            UserName = $"{agentId}@test.local",
            NormalizedUserName = $"{agentId}@test.local".ToUpperInvariant(),
            IsActive = true
        });
        await db.SaveChangesAsync();
        return agentId;
    }

    [Fact]
    public async Task CreateAsync_ForcesCompanyIdFromTenant_AndDefaultsStatusToNew()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid() };
        await using var db = CreateDb(dbName, tenant);
        var service = new LeadService(db, tenant, new NoOpNotificationService());

        var created = await service.CreateAsync(new CreateLeadRequest
        {
            FullName = "Jane Buyer",
            Source = LeadSource.Website
        });

        Assert.Equal(LeadStatus.New, created.Status);

        // Read it back scoped to the same tenant to confirm CompanyId was actually persisted correctly.
        var fetched = await service.GetByIdAsync(created.Id);
        Assert.Equal(created.Id, fetched.Id);
    }

    [Fact]
    public async Task CompanyA_CannotRead_CompanyBLead()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyAId = Guid.NewGuid();
        var companyBId = Guid.NewGuid();

        var tenantB = new FakeCurrentTenantService { CompanyId = companyBId, UserId = Guid.NewGuid() };
        Guid leadBId;
        await using (var dbB = CreateDb(dbName, tenantB))
        {
            var leadB = await new LeadService(dbB, tenantB, new NoOpNotificationService()).CreateAsync(new CreateLeadRequest { FullName = "Company B Lead", Source = LeadSource.Referral });
            leadBId = leadB.Id;
        }

        var tenantA = new FakeCurrentTenantService { CompanyId = companyAId, UserId = Guid.NewGuid() };
        await using var dbA = CreateDb(dbName, tenantA);
        var serviceA = new LeadService(dbA, tenantA, new NoOpNotificationService());

        var ex = await Assert.ThrowsAsync<AppException>(() => serviceA.GetByIdAsync(leadBId));
        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public async Task AssignAsync_Succeeds_ThenFails_WhenAlreadyAssigned()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid() };
        await using var db = CreateDb(dbName, tenant);
        var notificationService = new NoOpNotificationService();
        var service = new LeadService(db, tenant, notificationService);

        var agentId = await SeedAgentAsync(db, companyId);
        var lead = await service.CreateAsync(new CreateLeadRequest { FullName = "Lead", Source = LeadSource.Google });

        var assigned = await service.AssignAsync(lead.Id, new AssignLeadRequest { AgentId = agentId });
        Assert.Equal(agentId, assigned.AssignedAgentId);

        var notification = Assert.Single(notificationService.Sent);
        Assert.Equal(agentId, notification.UserId);
        Assert.Equal("LeadAssigned", notification.Type);

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            service.AssignAsync(lead.Id, new AssignLeadRequest { AgentId = agentId }));
        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task TransferAsync_Fails_WhenLeadHasNoCurrentAgent()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid() };
        await using var db = CreateDb(dbName, tenant);
        var service = new LeadService(db, tenant, new NoOpNotificationService());

        var agentId = await SeedAgentAsync(db, companyId);
        var lead = await service.CreateAsync(new CreateLeadRequest { FullName = "Lead", Source = LeadSource.Google });

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            service.TransferAsync(lead.Id, new AssignLeadRequest { AgentId = agentId }));
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task TransferAsync_Succeeds_AndLogsActivity()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid() };
        await using var db = CreateDb(dbName, tenant);
        var notificationService = new NoOpNotificationService();
        var service = new LeadService(db, tenant, notificationService);
        var activityService = new LeadActivityService(db, tenant);

        var agent1 = await SeedAgentAsync(db, companyId);
        var agent2 = await SeedAgentAsync(db, companyId);
        var lead = await service.CreateAsync(new CreateLeadRequest { FullName = "Lead", Source = LeadSource.Google });
        await service.AssignAsync(lead.Id, new AssignLeadRequest { AgentId = agent1 });

        var transferred = await service.TransferAsync(lead.Id, new AssignLeadRequest { AgentId = agent2 });

        Assert.Equal(agent2, transferred.AssignedAgentId);
        Assert.Contains(notificationService.Sent, n => n.UserId == agent2 && n.Type == "LeadAssigned");

        var timeline = await activityService.GetTimelineAsync(lead.Id);
        Assert.Contains(timeline, a => a.Type == LeadActivityType.Note);
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletes_LeadNoLongerVisible()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid() };
        await using var db = CreateDb(dbName, tenant);
        var service = new LeadService(db, tenant, new NoOpNotificationService());

        var lead = await service.CreateAsync(new CreateLeadRequest { FullName = "Lead", Source = LeadSource.Other });

        await service.DeleteAsync(lead.Id);

        var ex = await Assert.ThrowsAsync<AppException>(() => service.GetByIdAsync(lead.Id));
        Assert.Equal(404, ex.StatusCode);
    }
}
