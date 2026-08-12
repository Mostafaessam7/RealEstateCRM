using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RealEstateCRM.Application.Common.Exceptions;
using RealEstateCRM.Application.Projects.DTOs;
using RealEstateCRM.Infrastructure.Persistence;
using RealEstateCRM.Infrastructure.Projects;
using RealEstateCRM.Tests.MultiTenancy;
using Xunit;

namespace RealEstateCRM.Tests.Storage;

public class ProjectImageServiceTests
{
    private static readonly InMemoryDatabaseRoot Root = new();

    private static ApplicationDbContext CreateDb(string dbName, FakeCurrentTenantService tenant) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(dbName, Root).Options, tenant, new HttpContextAccessor());

    [Fact]
    public async Task UploadAsync_Fails_ForDisallowedContentType()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid() };
        await using var db = CreateDb(dbName, tenant);
        var project = await new ProjectService(db, tenant).CreateAsync(new CreateProjectRequest { Name = "P" });

        var service = new ProjectImageService(db, tenant, new InMemoryBlobStorageService());

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            service.UploadAsync(project.Id, stream, "malware.exe", "application/octet-stream", 3));

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task UploadAsync_Succeeds_AndPersistsTenantScopedPath()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid() };
        await using var db = CreateDb(dbName, tenant);
        var project = await new ProjectService(db, tenant).CreateAsync(new CreateProjectRequest { Name = "P" });

        var blobStorage = new InMemoryBlobStorageService();
        var service = new ProjectImageService(db, tenant, blobStorage);

        using var stream = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        var image = await service.UploadAsync(project.Id, stream, "front.jpg", "image/jpeg", 4);

        Assert.Equal(project.Id, image.ProjectId);
        var uploadedPath = Assert.Single(blobStorage.UploadedPaths);
        Assert.StartsWith($"companies/{companyId}/projects/{project.Id}/", uploadedPath);

        var listed = await service.ListAsync(project.Id);
        Assert.Single(listed);
    }

    [Fact]
    public async Task DeleteAsync_RemovesRowAndDeletesBlob()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid() };
        await using var db = CreateDb(dbName, tenant);
        var project = await new ProjectService(db, tenant).CreateAsync(new CreateProjectRequest { Name = "P" });

        var blobStorage = new InMemoryBlobStorageService();
        var service = new ProjectImageService(db, tenant, blobStorage);

        using var stream = new MemoryStream(new byte[] { 1 });
        var image = await service.UploadAsync(project.Id, stream, "front.jpg", "image/jpeg", 1);

        await service.DeleteAsync(project.Id, image.Id);

        Assert.Single(blobStorage.DeletedPaths);
        Assert.Empty(await service.ListAsync(project.Id));
    }
}
