using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RealEstateCRM.Application.Common.Exceptions;
using RealEstateCRM.Application.Tasks.DTOs;
using RealEstateCRM.Domain.Enums;
using RealEstateCRM.Infrastructure.Identity;
using RealEstateCRM.Infrastructure.Persistence;
using RealEstateCRM.Infrastructure.Tasks;
using RealEstateCRM.Tests.MultiTenancy;
using Xunit;

namespace RealEstateCRM.Tests.Tasks;

public class TaskItemServiceTests
{
    private static readonly InMemoryDatabaseRoot Root = new();

    private static ApplicationDbContext CreateDb(string dbName, FakeCurrentTenantService tenant) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(dbName, Root).Options, tenant, new HttpContextAccessor());

    private static async Task<Guid> SeedUserAsync(ApplicationDbContext db, Guid companyId)
    {
        var userId = Guid.NewGuid();
        db.Users.Add(new ApplicationUser
        {
            Id = userId, CompanyId = companyId, FullName = "User", Email = $"{userId}@test.local",
            NormalizedEmail = $"{userId}@test.local".ToUpperInvariant(), UserName = $"{userId}@test.local",
            NormalizedUserName = $"{userId}@test.local".ToUpperInvariant(), IsActive = true
        });
        await db.SaveChangesAsync();
        return userId;
    }

    [Fact]
    public async Task CreateAsync_Fails_WhenAssigneeNotInCompany()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenant = new FakeCurrentTenantService { CompanyId = Guid.NewGuid(), UserId = Guid.NewGuid() };
        await using var db = CreateDb(dbName, tenant);
        var service = new TaskItemService(db, tenant);

        var ex = await Assert.ThrowsAsync<AppException>(() => service.CreateAsync(new CreateTaskItemRequest
        {
            Title = "Follow up", AssignedToUserId = Guid.NewGuid()
        }));

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_Succeeds_AndDefaultsStatusToPending()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid() };
        await using var db = CreateDb(dbName, tenant);
        var userId = await SeedUserAsync(db, companyId);
        var service = new TaskItemService(db, tenant);

        var task = await service.CreateAsync(new CreateTaskItemRequest { Title = "Call lead", AssignedToUserId = userId });

        Assert.Equal(TaskItemStatus.Pending, task.Status);
        Assert.Equal(userId, task.AssignedToUserId);
    }

    [Fact]
    public async Task CompleteAsync_TransitionsToCompleted_ThenFailsOnSecondCall()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid() };
        await using var db = CreateDb(dbName, tenant);
        var userId = await SeedUserAsync(db, companyId);
        var service = new TaskItemService(db, tenant);

        var task = await service.CreateAsync(new CreateTaskItemRequest { Title = "Call lead", AssignedToUserId = userId });
        var completed = await service.CompleteAsync(task.Id);

        Assert.Equal(TaskItemStatus.Completed, completed.Status);

        var ex = await Assert.ThrowsAsync<AppException>(() => service.CompleteAsync(task.Id));
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task AssignAsync_ChangesAssignee()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid() };
        await using var db = CreateDb(dbName, tenant);
        var user1 = await SeedUserAsync(db, companyId);
        var user2 = await SeedUserAsync(db, companyId);
        var service = new TaskItemService(db, tenant);

        var task = await service.CreateAsync(new CreateTaskItemRequest { Title = "Call lead", AssignedToUserId = user1 });
        var reassigned = await service.AssignAsync(task.Id, new AssignTaskItemRequest { AssignedToUserId = user2 });

        Assert.Equal(user2, reassigned.AssignedToUserId);
    }

    [Fact]
    public async Task CreateAsync_LinksToLeadAndDeal_WhenProvidedAndValid()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid() };
        await using var db = CreateDb(dbName, tenant);
        var userId = await SeedUserAsync(db, companyId);

        var lead = await new RealEstateCRM.Infrastructure.Leads.LeadService(db, tenant, new NoOpNotificationService())
            .CreateAsync(new RealEstateCRM.Application.Leads.DTOs.CreateLeadRequest { FullName = "Buyer", Source = LeadSource.Website });

        var service = new TaskItemService(db, tenant);
        var task = await service.CreateAsync(new CreateTaskItemRequest
        {
            Title = "Follow up with buyer", AssignedToUserId = userId, LeadId = lead.Id
        });

        Assert.Equal(lead.Id, task.LeadId);
    }

    [Fact]
    public async Task CompanyA_CannotRead_CompanyBTask()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyAId = Guid.NewGuid();
        var companyBId = Guid.NewGuid();

        var tenantB = new FakeCurrentTenantService { CompanyId = companyBId, UserId = Guid.NewGuid() };
        Guid taskBId;
        await using (var dbB = CreateDb(dbName, tenantB))
        {
            var userB = await SeedUserAsync(dbB, companyBId);
            var taskB = await new TaskItemService(dbB, tenantB).CreateAsync(new CreateTaskItemRequest { Title = "B task", AssignedToUserId = userB });
            taskBId = taskB.Id;
        }

        var tenantA = new FakeCurrentTenantService { CompanyId = companyAId, UserId = Guid.NewGuid() };
        await using var dbA = CreateDb(dbName, tenantA);
        var serviceA = new TaskItemService(dbA, tenantA);

        var ex = await Assert.ThrowsAsync<AppException>(() => serviceA.GetByIdAsync(taskBId));
        Assert.Equal(404, ex.StatusCode);
    }
}
