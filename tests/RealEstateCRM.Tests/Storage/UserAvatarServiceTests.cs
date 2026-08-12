using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RealEstateCRM.Application.Common.Exceptions;
using RealEstateCRM.Infrastructure.Identity;
using RealEstateCRM.Infrastructure.Persistence;
using RealEstateCRM.Infrastructure.Users;
using RealEstateCRM.Tests.MultiTenancy;
using Xunit;

namespace RealEstateCRM.Tests.Storage;

public class UserAvatarServiceTests
{
    private static readonly InMemoryDatabaseRoot Root = new();

    private static ApplicationDbContext CreateDb(string dbName, FakeCurrentTenantService tenant) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(dbName, Root).Options, tenant, new HttpContextAccessor());

    [Fact]
    public async Task UploadAvatarAsync_SetsUrlOnUser_AndDeletesPreviousBlobOnReupload()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = userId };
        await using var db = CreateDb(dbName, tenant);
        db.Users.Add(new ApplicationUser
        {
            Id = userId, CompanyId = companyId, FullName = "Agent", Email = "agent@test.local",
            NormalizedEmail = "AGENT@TEST.LOCAL", UserName = "agent@test.local", NormalizedUserName = "AGENT@TEST.LOCAL", IsActive = true
        });
        await db.SaveChangesAsync();

        var blobStorage = new InMemoryBlobStorageService();
        var service = new UserAvatarService(db, tenant, blobStorage);

        using var first = new MemoryStream(new byte[] { 1 });
        var url1 = await service.UploadAvatarAsync(userId, first, "a.jpg", "image/jpeg", 1);
        Assert.NotEmpty(url1);
        Assert.Empty(blobStorage.DeletedPaths);

        using var second = new MemoryStream(new byte[] { 2 });
        await service.UploadAvatarAsync(userId, second, "b.png", "image/png", 1);

        Assert.Single(blobStorage.DeletedPaths);

        var user = await db.Users.SingleAsync(u => u.Id == userId);
        Assert.NotNull(user.AvatarUrl);
    }

    [Fact]
    public async Task UploadAvatarAsync_Fails_WhenUserNotInTenant()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenant = new FakeCurrentTenantService { CompanyId = Guid.NewGuid(), UserId = Guid.NewGuid() };
        await using var db = CreateDb(dbName, tenant);
        var service = new UserAvatarService(db, tenant, new InMemoryBlobStorageService());

        using var stream = new MemoryStream(new byte[] { 1 });
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            service.UploadAvatarAsync(Guid.NewGuid(), stream, "a.jpg", "image/jpeg", 1));

        Assert.Equal(404, ex.StatusCode);
    }
}
