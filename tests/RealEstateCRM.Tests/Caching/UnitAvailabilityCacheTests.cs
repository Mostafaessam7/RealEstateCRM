using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RealEstateCRM.Application.Common.Caching;
using RealEstateCRM.Application.Deals.DTOs;
using RealEstateCRM.Application.Leads.DTOs;
using RealEstateCRM.Application.Projects.DTOs;
using RealEstateCRM.Application.Units.DTOs;
using RealEstateCRM.Domain.Constants;
using RealEstateCRM.Domain.Enums;
using RealEstateCRM.Infrastructure.Deals;
using RealEstateCRM.Infrastructure.Identity;
using RealEstateCRM.Infrastructure.Leads;
using RealEstateCRM.Infrastructure.Persistence;
using RealEstateCRM.Infrastructure.Projects;
using RealEstateCRM.Infrastructure.Units;
using RealEstateCRM.Tests.MultiTenancy;
using Xunit;

namespace RealEstateCRM.Tests.Caching;

public class UnitAvailabilityCacheTests
{
    private static readonly InMemoryDatabaseRoot Root = new();

    private static ApplicationDbContext CreateDb(string dbName, FakeCurrentTenantService tenant) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(dbName, Root).Options, tenant, new HttpContextAccessor());

    [Fact]
    public async Task GetAvailableAsync_PopulatesTenantScopedCacheKey()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid() };
        await using var db = CreateDb(dbName, tenant);
        var cache = new InMemoryCacheService();
        var service = new UnitService(db, tenant, cache);

        var project = await new ProjectService(db, tenant).CreateAsync(new CreateProjectRequest { Name = "P" });
        await service.CreateAsync(new CreateUnitRequest { ProjectId = project.Id, UnitCode = "A-1", Price = 1_000_000 });

        await service.GetAvailableAsync(projectId: null);

        var cached = await cache.GetAsync<IReadOnlyList<UnitDto>>(TenantCacheKeys.AvailableUnits(companyId));
        Assert.NotNull(cached);
        Assert.Single(cached!);
    }

    [Fact]
    public async Task CreatingAUnit_InvalidatesTheAvailableUnitsCache()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid() };
        await using var db = CreateDb(dbName, tenant);
        var cache = new InMemoryCacheService();
        var service = new UnitService(db, tenant, cache);
        var project = await new ProjectService(db, tenant).CreateAsync(new CreateProjectRequest { Name = "P" });

        // Warm the cache with zero units.
        var before = await service.GetAvailableAsync(projectId: null);
        Assert.Empty(before);

        await service.CreateAsync(new CreateUnitRequest { ProjectId = project.Id, UnitCode = "A-1", Price = 1_000_000 });

        var after = await service.GetAvailableAsync(projectId: null);
        Assert.Single(after);
    }

    [Fact]
    public async Task ReservingADeal_InvalidatesTheAvailableUnitsCache()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = agentId, Roles = { Roles.SalesAgent } };
        await using var db = CreateDb(dbName, tenant);
        db.Users.Add(new ApplicationUser
        {
            Id = agentId, CompanyId = companyId, FullName = "Self", Email = "self@test.local",
            NormalizedEmail = "SELF@TEST.LOCAL", UserName = "self@test.local", NormalizedUserName = "SELF@TEST.LOCAL", IsActive = true
        });
        await db.SaveChangesAsync();

        var cache = new InMemoryCacheService();
        var unitService = new UnitService(db, tenant, cache);
        var project = await new ProjectService(db, tenant).CreateAsync(new CreateProjectRequest { Name = "P" });
        var unit = await unitService.CreateAsync(new CreateUnitRequest { ProjectId = project.Id, UnitCode = "A-1", Price = 1_000_000 });
        var lead = await new LeadService(db, tenant, new NoOpNotificationService()).CreateAsync(new CreateLeadRequest { FullName = "Buyer", Source = LeadSource.Website });

        // Warm the cache while the unit is still available.
        var before = await unitService.GetAvailableAsync(projectId: null);
        Assert.Single(before);

        var dealService = new DealService(db, tenant, new NoOpNotificationService(), cache);
        var deal = await dealService.CreateAsync(new CreateDealRequest { LeadId = lead.Id, UnitId = unit.Id, DealValue = 1_000_000 });
        await dealService.ReserveAsync(deal.Id);

        var after = await unitService.GetAvailableAsync(projectId: null);
        Assert.Empty(after);
    }
}
