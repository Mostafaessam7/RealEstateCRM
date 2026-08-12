using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RealEstateCRM.Domain.Entities;
using RealEstateCRM.Infrastructure.Companies;
using RealEstateCRM.Infrastructure.Dashboard;
using RealEstateCRM.Infrastructure.Leads;
using RealEstateCRM.Infrastructure.Persistence;
using RealEstateCRM.Tests.MultiTenancy;
using Xunit;

namespace RealEstateCRM.Tests.Caching;

public class DashboardAndCompanyCacheTests
{
    private static readonly InMemoryDatabaseRoot Root = new();

    private static ApplicationDbContext CreateDb(string dbName, FakeCurrentTenantService tenant) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(dbName, Root).Options, tenant, new HttpContextAccessor());

    [Fact]
    public async Task DashboardSummary_ReflectsTenantData_AndIsServedFromCacheOnSecondCall()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid() };
        await using var db = CreateDb(dbName, tenant);
        var cache = new InMemoryCacheService();

        await new LeadService(db, tenant, new NoOpNotificationService())
            .CreateAsync(new RealEstateCRM.Application.Leads.DTOs.CreateLeadRequest { FullName = "Buyer", Source = RealEstateCRM.Domain.Enums.LeadSource.Website });

        var dashboardService = new DashboardService(db, tenant, cache);
        var first = await dashboardService.GetSummaryAsync();
        Assert.Equal(1, first.TotalLeads);

        // A second lead created after the cache is warm should NOT show up until the TTL expires —
        // proves the summary actually came from cache, not a fresh query.
        await new LeadService(db, tenant, new NoOpNotificationService())
            .CreateAsync(new RealEstateCRM.Application.Leads.DTOs.CreateLeadRequest { FullName = "Buyer 2", Source = RealEstateCRM.Domain.Enums.LeadSource.Website });

        var second = await dashboardService.GetSummaryAsync();
        Assert.Equal(1, second.TotalLeads);
    }

    [Fact]
    public async Task CompanySettings_AreCachedPerTenant()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid() };
        await using var db = CreateDb(dbName, tenant);
        db.Companies.Add(new Company { Id = companyId, Name = "Acme Realty", Slug = "acme", IsActive = true });
        await db.SaveChangesAsync();

        var cache = new InMemoryCacheService();
        var companyService = new CompanyService(db, tenant, cache);

        var result = await companyService.GetCurrentAsync();

        Assert.Equal("Acme Realty", result.Name);

        var cached = await cache.GetAsync<RealEstateCRM.Application.Companies.DTOs.CompanyDto>(
            RealEstateCRM.Application.Common.Caching.TenantCacheKeys.Settings(companyId));
        Assert.NotNull(cached);
    }
}
