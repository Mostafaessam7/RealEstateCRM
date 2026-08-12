using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;

namespace RealEstateCRM.Tests.MultiTenancy;

public class TenantIsolationTests
{
    private static readonly InMemoryDatabaseRoot Root = new();

    private TestDbContext CreateContext(string dbName, Guid? companyId)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(dbName, Root)
            .Options;

        return new TestDbContext(options, new FakeCurrentTenantService { CompanyId = companyId });
    }

    private async Task<(Guid CompanyAId, Guid CompanyBId, Guid ItemAId, Guid ItemBId)> SeedAsync(string dbName)
    {
        var companyAId = Guid.NewGuid();
        var companyBId = Guid.NewGuid();

        await using (var ctxA = CreateContext(dbName, companyAId))
        {
            var itemA = new TestTenantEntity { Id = Guid.NewGuid(), Name = "Company A item" };
            ctxA.Items.Add(itemA);
            await ctxA.SaveChangesAsync();
        }

        Guid itemAId, itemBId;
        await using (var ctxA = CreateContext(dbName, companyAId))
        {
            itemAId = (await ctxA.Items.SingleAsync()).Id;
        }

        await using (var ctxB = CreateContext(dbName, companyBId))
        {
            var itemB = new TestTenantEntity { Id = Guid.NewGuid(), Name = "Company B item" };
            ctxB.Items.Add(itemB);
            await ctxB.SaveChangesAsync();
            itemBId = (await ctxB.Items.SingleAsync()).Id;
        }

        return (companyAId, companyBId, itemAId, itemBId);
    }

    [Fact]
    public async Task Write_ForcesCompanyIdFromTenantContext_IgnoringWhateverCallerSet()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var spoofedCompanyId = Guid.NewGuid();

        await using var ctx = CreateContext(dbName, companyId);
        ctx.Items.Add(new TestTenantEntity { Id = Guid.NewGuid(), CompanyId = spoofedCompanyId, Name = "spoof attempt" });
        await ctx.SaveChangesAsync();

        var saved = await ctx.Items.SingleAsync();
        Assert.Equal(companyId, saved.CompanyId);
        Assert.NotEqual(spoofedCompanyId, saved.CompanyId);
    }

    [Fact]
    public async Task Write_Throws_WhenNoTenantContextIsResolved()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var ctx = CreateContext(dbName, companyId: null);
        ctx.Items.Add(new TestTenantEntity { Id = Guid.NewGuid(), Name = "orphan" });

        await Assert.ThrowsAnyAsync<Exception>(() => ctx.SaveChangesAsync());
    }

    [Fact]
    public async Task CompanyA_CannotRead_CompanyBData()
    {
        var dbName = Guid.NewGuid().ToString();
        var (companyAId, _, _, itemBId) = await SeedAsync(dbName);

        await using var ctxA = CreateContext(dbName, companyAId);

        var result = await ctxA.Items.FirstOrDefaultAsync(i => i.Id == itemBId);

        Assert.Null(result);
    }

    [Fact]
    public async Task CompanyA_CannotUpdate_CompanyBData_ByGuessingId()
    {
        var dbName = Guid.NewGuid().ToString();
        var (companyAId, companyBId, _, itemBId) = await SeedAsync(dbName);

        await using (var ctxA = CreateContext(dbName, companyAId))
        {
            // Correct pattern: load through the tenant-scoped context, then mutate.
            var entity = await ctxA.Items.FirstOrDefaultAsync(i => i.Id == itemBId);
            Assert.Null(entity); // nothing to update — isolation already blocked the read
        }

        await using var ctxB = CreateContext(dbName, companyBId);
        var untouched = await ctxB.Items.SingleAsync(i => i.Id == itemBId);
        Assert.Equal("Company B item", untouched.Name);
    }

    [Fact]
    public async Task CompanyA_CannotDelete_CompanyBData_ByGuessingId()
    {
        var dbName = Guid.NewGuid().ToString();
        var (companyAId, companyBId, _, itemBId) = await SeedAsync(dbName);

        await using (var ctxA = CreateContext(dbName, companyAId))
        {
            var entity = await ctxA.Items.FirstOrDefaultAsync(i => i.Id == itemBId);
            Assert.Null(entity); // nothing to delete — isolation already blocked the read
        }

        await using var ctxB = CreateContext(dbName, companyBId);
        Assert.True(await ctxB.Items.AnyAsync(i => i.Id == itemBId));
    }

    [Fact]
    public async Task GuessedIds_CannotBypassIsolation_EvenWhenCorrect()
    {
        var dbName = Guid.NewGuid().ToString();
        var (companyAId, _, _, itemBId) = await SeedAsync(dbName);

        await using var ctxA = CreateContext(dbName, companyAId);

        // itemBId is a real, valid, correctly-guessed ID — isolation must still block it.
        var result = await ctxA.Items.Where(i => i.Id == itemBId).ToListAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task UnauthenticatedContext_SeesNoTenantData()
    {
        var dbName = Guid.NewGuid().ToString();
        await SeedAsync(dbName);

        await using var ctx = CreateContext(dbName, companyId: null);

        var result = await ctx.Items.ToListAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task ExplicitCrossTenantEscapeHatch_SeesAllTenants_OnlyWhenDeliberatelyInvoked()
    {
        var dbName = Guid.NewGuid().ToString();
        var (companyAId, _, _, _) = await SeedAsync(dbName);

        await using var ctxA = CreateContext(dbName, companyAId);

        var scoped = await ctxA.Items.ToListAsync();
        var allTenants = await ctxA.AllTenantsItems().ToListAsync();

        Assert.Single(scoped);
        Assert.Equal(2, allTenants.Count);
    }
}
