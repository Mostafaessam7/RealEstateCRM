using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RealEstateCRM.Application.Common.Exceptions;
using RealEstateCRM.Application.Leads.DTOs;
using RealEstateCRM.Application.Units.DTOs;
using RealEstateCRM.Application.Users.DTOs;
using RealEstateCRM.Domain.Constants;
using RealEstateCRM.Domain.Entities;
using RealEstateCRM.Domain.Enums;
using RealEstateCRM.Infrastructure.Identity;
using RealEstateCRM.Infrastructure.Leads;
using RealEstateCRM.Infrastructure.Persistence;
using RealEstateCRM.Infrastructure.Projects;
using RealEstateCRM.Infrastructure.Subscriptions;
using RealEstateCRM.Infrastructure.Units;
using RealEstateCRM.Infrastructure.Users;
using RealEstateCRM.Application.Projects.DTOs;
using RealEstateCRM.Tests.MultiTenancy;
using Xunit;

namespace RealEstateCRM.Tests.Subscriptions;

public class SubscriptionLimitServiceTests
{
    private static readonly InMemoryDatabaseRoot Root = new();

    private static ApplicationDbContext CreateDb(string dbName, FakeCurrentTenantService tenant) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(dbName, Root).Options, tenant, new HttpContextAccessor());

    private static async Task SeedFreePlanAndSubscriptionAsync(ApplicationDbContext db, Guid companyId, int maxUsers, int maxLeads, int maxUnits)
    {
        var now = DateTime.UtcNow;
        var plan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(), Code = "free", Name = "Free", MonthlyPrice = 0,
            MaxUsers = maxUsers, MaxLeads = maxLeads, MaxUnits = maxUnits, IsActive = true,
            CreatedAt = now, UpdatedAt = now
        };
        db.SubscriptionPlans.Add(plan);
        db.CompanySubscriptions.Add(new CompanySubscription
        {
            Id = Guid.NewGuid(), CompanyId = companyId, PlanId = plan.Id, Status = SubscriptionStatus.Trialing,
            TrialEndsAt = now.AddDays(14), CurrentPeriodStart = now, CurrentPeriodEnd = now.AddDays(14),
            CreatedAt = now, UpdatedAt = now
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task CreateAsync_Fails_WhenLeadLimitReached()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid(), Roles = { Roles.SalesAgent } };
        await using var db = CreateDb(dbName, tenant);
        await SeedFreePlanAndSubscriptionAsync(db, companyId, maxUsers: 3, maxLeads: 1, maxUnits: 25);

        var limitService = new SubscriptionLimitService(db, tenant);
        var leadService = new LeadService(db, tenant, new NoOpNotificationService(), limitService);

        await leadService.CreateAsync(new CreateLeadRequest { FullName = "First", Source = LeadSource.Website });

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            leadService.CreateAsync(new CreateLeadRequest { FullName = "Second", Source = LeadSource.Website }));

        Assert.Equal(402, ex.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_Fails_WhenUnitLimitReached()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid(), Roles = { Roles.SalesAgent } };
        await using var db = CreateDb(dbName, tenant);
        await SeedFreePlanAndSubscriptionAsync(db, companyId, maxUsers: 3, maxLeads: 100, maxUnits: 1);

        var project = await new ProjectService(db, tenant).CreateAsync(new CreateProjectRequest { Name = "P" });
        var limitService = new SubscriptionLimitService(db, tenant);
        var unitService = new UnitService(db, tenant, new InMemoryCacheService(), limitService);

        await unitService.CreateAsync(new CreateUnitRequest { ProjectId = project.Id, UnitCode = "U-1", Price = 1_000_000 });

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            unitService.CreateAsync(new CreateUnitRequest { ProjectId = project.Id, UnitCode = "U-2", Price = 1_000_000 }));

        Assert.Equal(402, ex.StatusCode);
    }

    [Fact]
    public async Task EnsureCanAddLeadAsync_Allows_WhenNoSubscriptionProvisionedYet()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid(), Roles = { Roles.SalesAgent } };
        await using var db = CreateDb(dbName, tenant);

        var limitService = new SubscriptionLimitService(db, tenant);
        var leadService = new LeadService(db, tenant, new NoOpNotificationService(), limitService);

        // No CompanySubscription row exists — must not block.
        var lead = await leadService.CreateAsync(new CreateLeadRequest { FullName = "Buyer", Source = LeadSource.Website });

        Assert.NotEqual(Guid.Empty, lead.Id);
    }

    [Fact]
    public async Task EnsureCanAddLeadAsync_Fails_WhenSubscriptionCancelled()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid(), Roles = { Roles.SalesAgent } };
        await using var db = CreateDb(dbName, tenant);
        await SeedFreePlanAndSubscriptionAsync(db, companyId, maxUsers: 3, maxLeads: 100, maxUnits: 25);

        var subscription = await db.CompanySubscriptions.FirstAsync(s => s.CompanyId == companyId);
        subscription.Status = SubscriptionStatus.Cancelled;
        await db.SaveChangesAsync();

        var limitService = new SubscriptionLimitService(db, tenant);
        var leadService = new LeadService(db, tenant, new NoOpNotificationService(), limitService);

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            leadService.CreateAsync(new CreateLeadRequest { FullName = "Buyer", Source = LeadSource.Website }));

        Assert.Equal(402, ex.StatusCode);
    }
}
