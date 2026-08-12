using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RealEstateCRM.Application.ApiKeys.DTOs;
using RealEstateCRM.Domain.Constants;
using RealEstateCRM.Infrastructure.ApiKeys;
using RealEstateCRM.Infrastructure.Persistence;
using RealEstateCRM.Tests.MultiTenancy;
using Xunit;

namespace RealEstateCRM.Tests.ApiKeys;

public class ApiKeyServiceTests
{
    private static readonly InMemoryDatabaseRoot Root = new();

    private static ApplicationDbContext CreateDb(string dbName, FakeCurrentTenantService tenant) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(dbName, Root).Options, tenant, new HttpContextAccessor());

    [Fact]
    public void Hash_IsDeterministic_AndDoesNotLeakThePlaintext()
    {
        var key = ApiKeyHasher.GenerateKey();
        var hash1 = ApiKeyHasher.Hash(key);
        var hash2 = ApiKeyHasher.Hash(key);

        Assert.Equal(hash1, hash2);
        Assert.DoesNotContain(key, hash1);
        Assert.StartsWith("rcrm_", key);
    }

    [Fact]
    public async Task CreateAsync_ReturnsPlaintextKey_ButOnlyStoresItsHash()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid(), Roles = { Roles.CompanyAdmin } };
        await using var db = CreateDb(dbName, tenant);

        var service = new ApiKeyService(db, tenant);
        var created = await service.CreateAsync(new CreateApiKeyRequest { Name = "Mobile app", Scopes = "read,write" });

        Assert.StartsWith("rcrm_", created.PlaintextKey);
        Assert.True(created.IsActive);

        var stored = await db.ApiKeys.FirstAsync(k => k.Id == created.Id);
        Assert.Equal(ApiKeyHasher.Hash(created.PlaintextKey), stored.HashedKey);
        Assert.DoesNotContain(created.PlaintextKey, stored.HashedKey);
    }

    [Fact]
    public async Task RevokeAsync_DeactivatesTheKey()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid(), Roles = { Roles.CompanyAdmin } };
        await using var db = CreateDb(dbName, tenant);

        var service = new ApiKeyService(db, tenant);
        var created = await service.CreateAsync(new CreateApiKeyRequest { Name = "X", Scopes = "read" });

        await service.RevokeAsync(created.Id);

        var keys = await service.ListAsync();
        Assert.False(keys.Single(k => k.Id == created.Id).IsActive);
    }
}
