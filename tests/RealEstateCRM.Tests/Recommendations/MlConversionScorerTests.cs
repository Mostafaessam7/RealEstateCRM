using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RealEstateCRM.Domain.Constants;
using RealEstateCRM.Domain.Entities;
using RealEstateCRM.Domain.Enums;
using RealEstateCRM.Infrastructure.Identity;
using RealEstateCRM.Infrastructure.Persistence;
using RealEstateCRM.Infrastructure.Recommendations;
using RealEstateCRM.Tests.MultiTenancy;
using Xunit;

namespace RealEstateCRM.Tests.Recommendations;

public class MlConversionScorerTests
{
    private static readonly InMemoryDatabaseRoot Root = new();

    private static ApplicationDbContext CreateDb(string dbName, FakeCurrentTenantService tenant) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(dbName, Root).Options, tenant, new HttpContextAccessor());

    private static async Task SeedResolvedDealAsync(
        ApplicationDbContext db, Guid companyId, Guid agentId, string location, string propertyType, decimal price, DealStatus status,
        bool withFeatureSnapshot = true)
    {
        var lead = new Lead
        {
            Id = Guid.NewGuid(), CompanyId = companyId, FullName = "Historical Lead", Source = LeadSource.Website,
            Status = LeadStatus.Lost, PreferredLocation = location, PropertyType = propertyType,
            BudgetMin = price * 0.9m, BudgetMax = price * 1.1m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        var unit = new Unit
        {
            Id = Guid.NewGuid(), CompanyId = companyId, ProjectId = Guid.NewGuid(), UnitCode = "H-" + Guid.NewGuid().ToString("N")[..6],
            Price = price, Location = location, PropertyType = propertyType, Status = UnitStatus.Sold,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };

        // Mirrors what DealService.CreateAsync actually does: snapshot the match features once,
        // at deal-creation time, rather than letting MlConversionScorer join current Lead/Unit
        // state (which is what the "advanced recommendation engine" scope limitation used to be).
        var deal = new Deal
        {
            Id = Guid.NewGuid(), CompanyId = companyId, LeadId = lead.Id, UnitId = unit.Id, SalesAgentId = agentId,
            DealValue = price, Status = status, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };

        if (withFeatureSnapshot)
        {
            var features = LeadUnitFeatureCalculator.Compute(lead, unit);
            deal.FeatureSnapshotBudgetFit = features.BudgetFit;
            deal.FeatureSnapshotLocationMatch = features.LocationMatch;
            deal.FeatureSnapshotPropertyTypeMatch = features.PropertyTypeMatch;
            deal.FeatureSnapshotPriceToBudgetRatio = features.PriceToBudgetRatio;
        }

        db.Leads.Add(lead);
        db.Units.Add(unit);
        db.Deals.Add(deal);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task TryScoreAsync_ReturnsNull_WhenTooFewResolvedDeals()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId };
        await using var db = CreateDb(dbName, tenant);

        var agentId = Guid.NewGuid();
        db.Users.Add(new ApplicationUser { Id = agentId, CompanyId = companyId, FullName = "Agent", Email = "a@test.local", NormalizedEmail = "A@TEST.LOCAL", UserName = "a@test.local", NormalizedUserName = "A@TEST.LOCAL", IsActive = true });
        await db.SaveChangesAsync();

        for (var i = 0; i < 5; i++)
        {
            await SeedResolvedDealAsync(db, companyId, agentId, "New Cairo", "Apartment", 1_000_000, DealStatus.Contracted);
        }

        var lead = new Lead { Id = Guid.NewGuid(), CompanyId = companyId, FullName = "New Lead", Source = LeadSource.Website, Status = LeadStatus.New, PreferredLocation = "New Cairo", PropertyType = "Apartment", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var candidate = new Unit { Id = Guid.NewGuid(), CompanyId = companyId, ProjectId = Guid.NewGuid(), UnitCode = "C-1", Price = 1_000_000, Location = "New Cairo", PropertyType = "Apartment", Status = UnitStatus.Available, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        var scorer = new MlConversionScorer(db);
        var scores = await scorer.TryScoreAsync(companyId, lead, new List<Unit> { candidate }, CancellationToken.None);

        Assert.Null(scores);
    }

    [Fact]
    public async Task TryScoreAsync_TrainsAndScores_WhenEnoughResolvedDeals()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId };
        await using var db = CreateDb(dbName, tenant);

        var agentId = Guid.NewGuid();
        db.Users.Add(new ApplicationUser { Id = agentId, CompanyId = companyId, FullName = "Agent", Email = "a@test.local", NormalizedEmail = "A@TEST.LOCAL", UserName = "a@test.local", NormalizedUserName = "A@TEST.LOCAL", IsActive = true });
        await db.SaveChangesAsync();

        // 8 well-matched deals converted, 4 mismatched deals cancelled — enough signal to train on.
        for (var i = 0; i < 8; i++)
        {
            await SeedResolvedDealAsync(db, companyId, agentId, "New Cairo", "Apartment", 1_000_000, DealStatus.Contracted);
        }
        for (var i = 0; i < 4; i++)
        {
            await SeedResolvedDealAsync(db, companyId, agentId, "Alexandria", "Villa", 5_000_000, DealStatus.Cancelled);
        }

        var lead = new Lead { Id = Guid.NewGuid(), CompanyId = companyId, FullName = "New Lead", Source = LeadSource.Website, Status = LeadStatus.New, PreferredLocation = "New Cairo", PropertyType = "Apartment", BudgetMin = 900_000, BudgetMax = 1_100_000, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var candidate = new Unit { Id = Guid.NewGuid(), CompanyId = companyId, ProjectId = Guid.NewGuid(), UnitCode = "C-1", Price = 1_000_000, Location = "New Cairo", PropertyType = "Apartment", Status = UnitStatus.Available, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        var scorer = new MlConversionScorer(db);
        var scores = await scorer.TryScoreAsync(companyId, lead, new List<Unit> { candidate }, CancellationToken.None);

        Assert.NotNull(scores);
        Assert.True(scores!.ContainsKey(candidate.Id));
        Assert.InRange(scores[candidate.Id], 0f, 1f);
    }

    [Fact]
    public async Task TryScoreAsync_ExcludesDealsWithoutAFeatureSnapshot_FromTrainingCount()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId };
        await using var db = CreateDb(dbName, tenant);

        var agentId = Guid.NewGuid();
        db.Users.Add(new ApplicationUser { Id = agentId, CompanyId = companyId, FullName = "Agent", Email = "a@test.local", NormalizedEmail = "A@TEST.LOCAL", UserName = "a@test.local", NormalizedUserName = "A@TEST.LOCAL", IsActive = true });
        await db.SaveChangesAsync();

        // 12 resolved deals total, but only 4 carry a feature snapshot (as if created before
        // this column existed) — below MinTrainingDeals once the unsnapshotted ones are
        // correctly excluded, so this must behave exactly like "too few resolved deals".
        for (var i = 0; i < 4; i++)
        {
            await SeedResolvedDealAsync(db, companyId, agentId, "New Cairo", "Apartment", 1_000_000, DealStatus.Contracted, withFeatureSnapshot: true);
        }
        for (var i = 0; i < 8; i++)
        {
            await SeedResolvedDealAsync(db, companyId, agentId, "New Cairo", "Apartment", 1_000_000, DealStatus.Contracted, withFeatureSnapshot: false);
        }

        var lead = new Lead { Id = Guid.NewGuid(), CompanyId = companyId, FullName = "New Lead", Source = LeadSource.Website, Status = LeadStatus.New, PreferredLocation = "New Cairo", PropertyType = "Apartment", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var candidate = new Unit { Id = Guid.NewGuid(), CompanyId = companyId, ProjectId = Guid.NewGuid(), UnitCode = "C-1", Price = 1_000_000, Location = "New Cairo", PropertyType = "Apartment", Status = UnitStatus.Available, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        var scorer = new MlConversionScorer(db);
        var scores = await scorer.TryScoreAsync(companyId, lead, new List<Unit> { candidate }, CancellationToken.None);

        Assert.Null(scores);
    }
}
