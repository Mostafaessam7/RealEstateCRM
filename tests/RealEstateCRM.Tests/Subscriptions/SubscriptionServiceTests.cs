using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RealEstateCRM.Application.Common.Exceptions;
using RealEstateCRM.Application.Subscriptions.DTOs;
using RealEstateCRM.Domain.Constants;
using RealEstateCRM.Domain.Entities;
using RealEstateCRM.Domain.Enums;
using RealEstateCRM.Infrastructure.Persistence;
using RealEstateCRM.Infrastructure.Subscriptions;
using RealEstateCRM.Tests.MultiTenancy;
using Xunit;

namespace RealEstateCRM.Tests.Subscriptions;

public class SubscriptionServiceTests
{
    private static readonly InMemoryDatabaseRoot Root = new();

    private static ApplicationDbContext CreateDb(string dbName, FakeCurrentTenantService tenant) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(dbName, Root).Options, tenant, new HttpContextAccessor());

    private static async Task SeedPlansAsync(ApplicationDbContext db)
    {
        var now = DateTime.UtcNow;
        db.SubscriptionPlans.AddRange(
            new SubscriptionPlan { Id = Guid.NewGuid(), Code = "free", Name = "Free", MonthlyPrice = 0, MaxUsers = 3, MaxLeads = 100, MaxUnits = 25, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new SubscriptionPlan { Id = Guid.NewGuid(), Code = "starter", Name = "Starter", MonthlyPrice = 49, MaxUsers = 10, MaxLeads = 1000, MaxUnits = 200, IsActive = true, CreatedAt = now, UpdatedAt = now });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetCurrentAsync_AutoProvisions_FreeTrialSubscription_OnFirstAccess()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid(), Roles = { Roles.CompanyAdmin } };
        await using var db = CreateDb(dbName, tenant);
        await SeedPlansAsync(db);

        var service = new SubscriptionService(db, tenant);
        var subscription = await service.GetCurrentAsync();

        Assert.Equal("free", subscription.Plan.Code);
        Assert.Equal(SubscriptionStatus.Trialing, subscription.Status);
        Assert.True(subscription.TrialEndsAt > DateTime.UtcNow);

        // Second call must reuse the same row, not create another.
        var again = await service.GetCurrentAsync();
        Assert.Equal(subscription.Id, again.Id);
        Assert.Single(db.CompanySubscriptions.Where(s => s.CompanyId == companyId));
    }

    [Fact]
    public async Task GetCurrentAsync_ReportsUsage_AgainstPlanLimits()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid(), Roles = { Roles.CompanyAdmin } };
        await using var db = CreateDb(dbName, tenant);
        await SeedPlansAsync(db);

        db.Users.Add(new Infrastructure.Identity.ApplicationUser
        {
            Id = Guid.NewGuid(), CompanyId = companyId, FullName = "Agent", Email = "a@test.local",
            NormalizedEmail = "A@TEST.LOCAL", UserName = "a@test.local", NormalizedUserName = "A@TEST.LOCAL", IsActive = true
        });
        await db.SaveChangesAsync();

        var service = new SubscriptionService(db, tenant);
        var subscription = await service.GetCurrentAsync();

        Assert.Equal(1, subscription.Usage.UserCount);
        Assert.Equal(0, subscription.Usage.LeadCount);
        Assert.Equal(3, subscription.Plan.MaxUsers);
    }

    [Fact]
    public async Task ChangePlanAsync_SwitchesPlan_AndActivatesSubscription()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid(), Roles = { Roles.CompanyAdmin } };
        await using var db = CreateDb(dbName, tenant);
        await SeedPlansAsync(db);

        var service = new SubscriptionService(db, tenant);
        await service.GetCurrentAsync();

        var updated = await service.ChangePlanAsync(new ChangePlanRequest { PlanCode = "starter" });

        Assert.Equal("starter", updated.Plan.Code);
        Assert.Equal(SubscriptionStatus.Active, updated.Status);
    }

    [Fact]
    public async Task ChangePlanAsync_Fails_ForSalesAgent()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid(), Roles = { Roles.SalesAgent } };
        await using var db = CreateDb(dbName, tenant);
        await SeedPlansAsync(db);

        var service = new SubscriptionService(db, tenant);

        var ex = await Assert.ThrowsAsync<AppException>(() => service.ChangePlanAsync(new ChangePlanRequest { PlanCode = "starter" }));
        Assert.Equal(403, ex.StatusCode);
    }

    [Fact]
    public async Task CancelAsync_Fails_WhenAlreadyCancelled()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid(), Roles = { Roles.CompanyAdmin } };
        await using var db = CreateDb(dbName, tenant);
        await SeedPlansAsync(db);

        var service = new SubscriptionService(db, tenant);
        await service.GetCurrentAsync();
        await service.CancelAsync();

        var ex = await Assert.ThrowsAsync<AppException>(() => service.CancelAsync());
        Assert.Equal(400, ex.StatusCode);
    }
}
