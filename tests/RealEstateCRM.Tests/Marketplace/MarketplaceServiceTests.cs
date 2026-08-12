using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RealEstateCRM.Application.Marketplace.DTOs;
using RealEstateCRM.Application.Projects.DTOs;
using RealEstateCRM.Application.Units.DTOs;
using RealEstateCRM.Domain.Constants;
using RealEstateCRM.Infrastructure.Marketplace;
using RealEstateCRM.Infrastructure.Persistence;
using RealEstateCRM.Infrastructure.Projects;
using RealEstateCRM.Infrastructure.Units;
using RealEstateCRM.Tests.MultiTenancy;
using Xunit;

namespace RealEstateCRM.Tests.Marketplace;

public class MarketplaceServiceTests
{
    private static readonly InMemoryDatabaseRoot Root = new();

    private static ApplicationDbContext CreateDb(string dbName, FakeCurrentTenantService tenant) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(dbName, Root).Options, tenant, new HttpContextAccessor());

    [Fact]
    public async Task ListAsync_ReturnsOnlyPubliclyListedAvailableUnits_AcrossCompanies()
    {
        var dbNameA = Guid.NewGuid().ToString();
        var companyA = Guid.NewGuid();
        var tenantA = new FakeCurrentTenantService { CompanyId = companyA, UserId = Guid.NewGuid(), Roles = { Roles.CompanyAdmin } };
        var root = new InMemoryDatabaseRoot();
        var sharedDbName = "marketplace-" + Guid.NewGuid();

        await using var dbA = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(sharedDbName, root).Options, tenantA, new HttpContextAccessor());

        var companyRow = new Domain.Entities.Company { Id = companyA, Name = "Acme Realty", Slug = "acme", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        dbA.Companies.Add(companyRow);
        await dbA.SaveChangesAsync();

        var project = await new ProjectService(dbA, tenantA).CreateAsync(new CreateProjectRequest { Name = "Palm Towers" });
        var unitService = new UnitService(dbA, tenantA, new InMemoryCacheService());

        var listedUnit = await unitService.CreateAsync(new CreateUnitRequest
        {
            ProjectId = project.Id, UnitCode = "L-1", Price = 1_500_000, Location = "New Cairo", IsPubliclyListed = true
        });
        await unitService.CreateAsync(new CreateUnitRequest
        {
            ProjectId = project.Id, UnitCode = "L-2", Price = 1_500_000, Location = "New Cairo", IsPubliclyListed = false
        });

        var marketplaceService = new MarketplaceService(dbA);
        var result = await marketplaceService.ListAsync(new PublicUnitListQuery());

        Assert.Single(result.Items);
        Assert.Equal(listedUnit.Id, result.Items[0].UnitId);
        Assert.Equal("Palm Towers", result.Items[0].ProjectName);
        Assert.Equal("Acme Realty", result.Items[0].CompanyName);
    }

    [Fact]
    public async Task ListAsync_FiltersByPriceRange()
    {
        var companyA = Guid.NewGuid();
        var tenantA = new FakeCurrentTenantService { CompanyId = companyA, UserId = Guid.NewGuid(), Roles = { Roles.CompanyAdmin } };
        var root = new InMemoryDatabaseRoot();
        var sharedDbName = "marketplace-price-" + Guid.NewGuid();

        await using var dbA = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(sharedDbName, root).Options, tenantA, new HttpContextAccessor());

        dbA.Companies.Add(new Domain.Entities.Company { Id = companyA, Name = "Acme", Slug = "acme", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await dbA.SaveChangesAsync();

        var project = await new ProjectService(dbA, tenantA).CreateAsync(new CreateProjectRequest { Name = "P" });
        var unitService = new UnitService(dbA, tenantA, new InMemoryCacheService());

        await unitService.CreateAsync(new CreateUnitRequest { ProjectId = project.Id, UnitCode = "Cheap", Price = 500_000, IsPubliclyListed = true });
        var expensive = await unitService.CreateAsync(new CreateUnitRequest { ProjectId = project.Id, UnitCode = "Expensive", Price = 5_000_000, IsPubliclyListed = true });

        var marketplaceService = new MarketplaceService(dbA);
        var result = await marketplaceService.ListAsync(new PublicUnitListQuery { MinPrice = 1_000_000 });

        Assert.Single(result.Items);
        Assert.Equal(expensive.Id, result.Items[0].UnitId);
    }
}
