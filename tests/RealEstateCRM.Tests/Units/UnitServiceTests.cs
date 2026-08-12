using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RealEstateCRM.Application.Common.Exceptions;
using RealEstateCRM.Application.Projects.DTOs;
using RealEstateCRM.Application.Units.DTOs;
using RealEstateCRM.Domain.Enums;
using RealEstateCRM.Infrastructure.Persistence;
using RealEstateCRM.Infrastructure.Projects;
using RealEstateCRM.Infrastructure.Units;
using RealEstateCRM.Tests.MultiTenancy;
using Xunit;

namespace RealEstateCRM.Tests.Units;

public class UnitServiceTests
{
    private static readonly InMemoryDatabaseRoot Root = new();

    private static ApplicationDbContext CreateDb(string dbName, FakeCurrentTenantService tenant) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(dbName, Root).Options, tenant, new HttpContextAccessor());

    private static async Task<Guid> SeedProjectAsync(ApplicationDbContext db, FakeCurrentTenantService tenant)
    {
        var project = await new ProjectService(db, tenant).CreateAsync(new CreateProjectRequest { Name = "Project " + Guid.NewGuid() });
        return project.Id;
    }

    [Fact]
    public async Task CreateAsync_Fails_WhenProjectDoesNotExistInTenant()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenant = new FakeCurrentTenantService { CompanyId = Guid.NewGuid(), UserId = Guid.NewGuid() };
        await using var db = CreateDb(dbName, tenant);
        var service = new UnitService(db, tenant, new InMemoryCacheService());

        var ex = await Assert.ThrowsAsync<AppException>(() => service.CreateAsync(new CreateUnitRequest
        {
            ProjectId = Guid.NewGuid(),
            UnitCode = "A-101",
            Price = 1_000_000
        }));

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_Fails_WhenUnitCodeDuplicatedWithinProject()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenant = new FakeCurrentTenantService { CompanyId = Guid.NewGuid(), UserId = Guid.NewGuid() };
        await using var db = CreateDb(dbName, tenant);
        var service = new UnitService(db, tenant, new InMemoryCacheService());
        var projectId = await SeedProjectAsync(db, tenant);

        await service.CreateAsync(new CreateUnitRequest { ProjectId = projectId, UnitCode = "A-101", Price = 1_000_000 });

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            service.CreateAsync(new CreateUnitRequest { ProjectId = projectId, UnitCode = "A-101", Price = 2_000_000 }));

        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task GetAvailableAsync_ReturnsOnlyAvailableUnits_ScopedToProject()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenant = new FakeCurrentTenantService { CompanyId = Guid.NewGuid(), UserId = Guid.NewGuid() };
        await using var db = CreateDb(dbName, tenant);
        var service = new UnitService(db, tenant, new InMemoryCacheService());
        var projectId = await SeedProjectAsync(db, tenant);
        var otherProjectId = await SeedProjectAsync(db, tenant);

        var available = await service.CreateAsync(new CreateUnitRequest
        {
            ProjectId = projectId, UnitCode = "AVAIL-1", Price = 1_000_000, Status = UnitStatus.Available
        });
        await service.CreateAsync(new CreateUnitRequest
        {
            ProjectId = projectId, UnitCode = "SOLD-1", Price = 1_000_000, Status = UnitStatus.Sold
        });
        await service.CreateAsync(new CreateUnitRequest
        {
            ProjectId = otherProjectId, UnitCode = "AVAIL-2", Price = 1_000_000, Status = UnitStatus.Available
        });

        var results = await service.GetAvailableAsync(projectId);

        var dto = Assert.Single(results);
        Assert.Equal(available.Id, dto.Id);
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletes_UnitNoLongerVisible()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenant = new FakeCurrentTenantService { CompanyId = Guid.NewGuid(), UserId = Guid.NewGuid() };
        await using var db = CreateDb(dbName, tenant);
        var service = new UnitService(db, tenant, new InMemoryCacheService());
        var projectId = await SeedProjectAsync(db, tenant);

        var unit = await service.CreateAsync(new CreateUnitRequest { ProjectId = projectId, UnitCode = "DEL-1", Price = 1_000_000 });
        await service.DeleteAsync(unit.Id);

        var ex = await Assert.ThrowsAsync<AppException>(() => service.GetByIdAsync(unit.Id));
        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenant = new FakeCurrentTenantService { CompanyId = Guid.NewGuid(), UserId = Guid.NewGuid() };
        await using var db = CreateDb(dbName, tenant);
        var service = new UnitService(db, tenant, new InMemoryCacheService());
        var projectId = await SeedProjectAsync(db, tenant);
        var unit = await service.CreateAsync(new CreateUnitRequest { ProjectId = projectId, UnitCode = "UPD-1", Price = 1_000_000 });

        var updated = await service.UpdateAsync(unit.Id, new UpdateUnitRequest
        {
            ProjectId = projectId, UnitCode = "UPD-1", Price = 1_250_000, Status = UnitStatus.Unavailable
        });

        Assert.Equal(1_250_000, updated.Price);
        Assert.Equal(UnitStatus.Unavailable, updated.Status);
    }

    [Fact]
    public async Task UpdateAsync_ThrowsConflict_NotDuplicateCodeMessage_WhenConcurrentlyModified()
    {
        // DbUpdateConcurrencyException derives from DbUpdateException — regression test for the
        // catch-order bug this fix corrected: before the fix, a genuine concurrency conflict on
        // UpdateAsync was caught by the (broader) DbUpdateException handler and mislabeled as
        // "A unit with this code already exists in this project," which is both wrong and
        // actively misleading to the caller.
        var dbName = Guid.NewGuid().ToString();
        var tenant = new FakeCurrentTenantService { CompanyId = Guid.NewGuid(), UserId = Guid.NewGuid() };
        await using var db1 = CreateDb(dbName, tenant);
        var service1 = new UnitService(db1, tenant, new InMemoryCacheService());
        var projectId = await SeedProjectAsync(db1, tenant);
        var unit = await service1.CreateAsync(new CreateUnitRequest { ProjectId = projectId, UnitCode = "RACE-1", Price = 1_000_000 });

        // db1 tracks the unit as it stood right after creation.
        await db1.Units.FirstOrDefaultAsync(u => u.Id == unit.Id);

        // A second, independent request updates and commits first.
        await using var db2 = CreateDb(dbName, tenant);
        var unitViaDb2 = await db2.Units.FirstOrDefaultAsync(u => u.Id == unit.Id);
        unitViaDb2!.Price = 900_000;
        unitViaDb2.UpdatedAt = DateTime.UtcNow;
        await db2.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<AppException>(() => service1.UpdateAsync(unit.Id, new UpdateUnitRequest
        {
            ProjectId = projectId, UnitCode = "RACE-1", Price = 1_100_000
        }));

        Assert.Equal(409, ex.StatusCode);
        Assert.Contains("updated by someone else", ex.Message);
        Assert.DoesNotContain("already exists", ex.Message);
    }
}
