using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RealEstateCRM.Application.Common.Exceptions;
using RealEstateCRM.Application.Leads.DTOs;
using RealEstateCRM.Application.Projects.DTOs;
using RealEstateCRM.Application.Units.DTOs;
using RealEstateCRM.Domain.Constants;
using RealEstateCRM.Domain.Enums;
using RealEstateCRM.Infrastructure.Caching;
using RealEstateCRM.Infrastructure.Leads;
using RealEstateCRM.Infrastructure.Persistence;
using RealEstateCRM.Infrastructure.Projects;
using RealEstateCRM.Infrastructure.Recommendations;
using RealEstateCRM.Infrastructure.Units;
using RealEstateCRM.Tests.MultiTenancy;
using Xunit;

namespace RealEstateCRM.Tests.Recommendations;

public class RecommendationServiceTests
{
    private static readonly InMemoryDatabaseRoot Root = new();

    private static ApplicationDbContext CreateDb(string dbName, FakeCurrentTenantService tenant) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(dbName, Root).Options, tenant, new HttpContextAccessor());

    [Fact]
    public async Task GetRecommendationsForLeadAsync_RanksBudgetLocationAndTypeMatchesHighest()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid(), Roles = { Roles.CompanyAdmin } };
        await using var db = CreateDb(dbName, tenant);

        var lead = await new LeadService(db, tenant, new NoOpNotificationService()).CreateAsync(new CreateLeadRequest
        {
            FullName = "Buyer", Source = LeadSource.Website,
            BudgetMin = 1_800_000, BudgetMax = 2_200_000,
            PreferredLocation = "New Cairo", PropertyType = "Apartment"
        });

        var project = await new ProjectService(db, tenant).CreateAsync(new CreateProjectRequest { Name = "P" });
        var unitService = new UnitService(db, tenant, new InMemoryCacheService());

        var perfectMatch = await unitService.CreateAsync(new CreateUnitRequest
        {
            ProjectId = project.Id, UnitCode = "U-1", Price = 2_000_000, Location = "New Cairo - Fifth Settlement", PropertyType = "Apartment"
        });
        var wrongLocation = await unitService.CreateAsync(new CreateUnitRequest
        {
            ProjectId = project.Id, UnitCode = "U-2", Price = 2_000_000, Location = "Alexandria", PropertyType = "Apartment"
        });
        var outOfBudget = await unitService.CreateAsync(new CreateUnitRequest
        {
            ProjectId = project.Id, UnitCode = "U-3", Price = 10_000_000, Location = "New Cairo", PropertyType = "Apartment"
        });

        var service = new RecommendationService(db);
        var recommendations = await service.GetRecommendationsForLeadAsync(lead.Id);

        Assert.Equal(perfectMatch.Id, recommendations[0].UnitId);
        Assert.Equal(100, recommendations[0].Score);
        Assert.Contains("Within budget", recommendations[0].MatchReasons);
        Assert.Contains("Preferred location", recommendations[0].MatchReasons);
        Assert.Contains("Matches property type", recommendations[0].MatchReasons);

        var outOfBudgetResult = recommendations.First(r => r.UnitId == outOfBudget.Id);
        Assert.DoesNotContain("Within budget", outOfBudgetResult.MatchReasons);

        var wrongLocationResult = recommendations.First(r => r.UnitId == wrongLocation.Id);
        Assert.DoesNotContain("Preferred location", wrongLocationResult.MatchReasons);
    }

    [Fact]
    public async Task GetRecommendationsForLeadAsync_ExcludesUnavailableUnits()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid(), Roles = { Roles.CompanyAdmin } };
        await using var db = CreateDb(dbName, tenant);

        var lead = await new LeadService(db, tenant, new NoOpNotificationService())
            .CreateAsync(new CreateLeadRequest { FullName = "Buyer", Source = LeadSource.Website });

        var project = await new ProjectService(db, tenant).CreateAsync(new CreateProjectRequest { Name = "P" });
        var unitService = new UnitService(db, tenant, new InMemoryCacheService());
        var soldUnit = await unitService.CreateAsync(new CreateUnitRequest { ProjectId = project.Id, UnitCode = "U-1", Price = 1_000_000, Status = UnitStatus.Sold });

        var service = new RecommendationService(db);
        var recommendations = await service.GetRecommendationsForLeadAsync(lead.Id);

        Assert.DoesNotContain(recommendations, r => r.UnitId == soldUnit.Id);
    }

    [Fact]
    public async Task GetRecommendationsForLeadAsync_Fails_WhenLeadNotFound()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid(), Roles = { Roles.CompanyAdmin } };
        await using var db = CreateDb(dbName, tenant);

        var service = new RecommendationService(db);

        var ex = await Assert.ThrowsAsync<AppException>(() => service.GetRecommendationsForLeadAsync(Guid.NewGuid()));
        Assert.Equal(404, ex.StatusCode);
    }
}
