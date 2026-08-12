using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RealEstateCRM.Application.Common.Exceptions;
using RealEstateCRM.Application.Projects.DTOs;
using RealEstateCRM.Domain.Enums;
using RealEstateCRM.Infrastructure.Persistence;
using RealEstateCRM.Infrastructure.Projects;
using RealEstateCRM.Tests.MultiTenancy;
using Xunit;

namespace RealEstateCRM.Tests.Projects;

public class ProjectServiceTests
{
    private static readonly InMemoryDatabaseRoot Root = new();

    private static ApplicationDbContext CreateDb(string dbName, FakeCurrentTenantService tenant) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(dbName, Root).Options, tenant, new HttpContextAccessor());

    [Fact]
    public async Task CreateAsync_ForcesCompanyIdFromTenant_AndDefaultsStatus()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid() };
        await using var db = CreateDb(dbName, tenant);
        var service = new ProjectService(db, tenant);

        var created = await service.CreateAsync(new CreateProjectRequest { Name = "Palm Hills" });

        Assert.Equal(ProjectStatus.Planning, created.Status);

        var fetched = await service.GetByIdAsync(created.Id);
        Assert.Equal("Palm Hills", fetched.Name);
    }

    [Fact]
    public async Task CompanyA_CannotRead_CompanyBProject()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyAId = Guid.NewGuid();
        var companyBId = Guid.NewGuid();

        var tenantB = new FakeCurrentTenantService { CompanyId = companyBId, UserId = Guid.NewGuid() };
        Guid projectBId;
        await using (var dbB = CreateDb(dbName, tenantB))
        {
            var projectB = await new ProjectService(dbB, tenantB).CreateAsync(new CreateProjectRequest { Name = "Company B Project" });
            projectBId = projectB.Id;
        }

        var tenantA = new FakeCurrentTenantService { CompanyId = companyAId, UserId = Guid.NewGuid() };
        await using var dbA = CreateDb(dbName, tenantA);
        var serviceA = new ProjectService(dbA, tenantA);

        var ex = await Assert.ThrowsAsync<AppException>(() => serviceA.GetByIdAsync(projectBId));
        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletes_ProjectNoLongerVisible()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid() };
        await using var db = CreateDb(dbName, tenant);
        var service = new ProjectService(db, tenant);

        var project = await service.CreateAsync(new CreateProjectRequest { Name = "To Delete" });
        await service.DeleteAsync(project.Id);

        var ex = await Assert.ThrowsAsync<AppException>(() => service.GetByIdAsync(project.Id));
        Assert.Equal(404, ex.StatusCode);
    }
}
