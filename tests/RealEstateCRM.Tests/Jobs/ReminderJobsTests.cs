using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RealEstateCRM.Application.Leads.DTOs;
using RealEstateCRM.Application.Tasks.DTOs;
using RealEstateCRM.Domain.Enums;
using RealEstateCRM.Infrastructure.Auth;
using RealEstateCRM.Infrastructure.Identity;
using RealEstateCRM.Infrastructure.Jobs;
using RealEstateCRM.Infrastructure.Leads;
using RealEstateCRM.Infrastructure.Persistence;
using RealEstateCRM.Infrastructure.Tasks;
using RealEstateCRM.Tests.MultiTenancy;
using Xunit;

namespace RealEstateCRM.Tests.Jobs;

public class ReminderJobsTests
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
    public async Task SendDueFollowUpRemindersAsync_CreatesNotification_ForDueFollowUp_AcrossTenants()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid() };
        await using var db = CreateDb(dbName, tenant);
        var agentId = await SeedUserAsync(db, companyId);

        var leadService = new LeadService(db, tenant, new NoOpNotificationService());
        var lead = await leadService.CreateAsync(new CreateLeadRequest { FullName = "Buyer", Source = LeadSource.Website });
        await leadService.AssignAsync(lead.Id, new AssignLeadRequest { AgentId = agentId });

        db.LeadActivities.Add(new RealEstateCRM.Domain.Entities.LeadActivity
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            LeadId = lead.Id,
            UserId = agentId,
            Type = LeadActivityType.FollowUp,
            ActivityDate = DateTime.UtcNow.AddMinutes(5), // due within the reminder window
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var notificationService = new NoOpNotificationService();
        var job = new ReminderJobs(db, new TenantScope(), notificationService);
        await job.SendDueFollowUpRemindersAsync();

        var notification = Assert.Single(notificationService.Sent);
        Assert.Equal(agentId, notification.UserId);
        Assert.Equal("FollowUpReminder", notification.Type);

        var activity = Assert.Single(db.ForAllTenants<RealEstateCRM.Domain.Entities.LeadActivity>());
        Assert.NotNull(activity.ReminderSentAt);
    }

    [Fact]
    public async Task SendDueFollowUpRemindersAsync_IsIdempotent_DoesNotDuplicateOnSecondRun()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid() };
        await using var db = CreateDb(dbName, tenant);
        var agentId = await SeedUserAsync(db, companyId);

        var leadService = new LeadService(db, tenant, new NoOpNotificationService());
        var lead = await leadService.CreateAsync(new CreateLeadRequest { FullName = "Buyer", Source = LeadSource.Website });
        await leadService.AssignAsync(lead.Id, new AssignLeadRequest { AgentId = agentId });

        db.LeadActivities.Add(new RealEstateCRM.Domain.Entities.LeadActivity
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            LeadId = lead.Id,
            UserId = agentId,
            Type = LeadActivityType.FollowUp,
            ActivityDate = DateTime.UtcNow.AddMinutes(-5), // overdue
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var notificationService = new NoOpNotificationService();
        var job = new ReminderJobs(db, new TenantScope(), notificationService);
        await job.SendDueFollowUpRemindersAsync();
        await job.SendDueFollowUpRemindersAsync();

        Assert.Single(notificationService.Sent);
    }

    [Fact]
    public async Task SendDueTaskRemindersAsync_CreatesNotification_ForDueTask()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid() };
        await using var db = CreateDb(dbName, tenant);
        var userId = await SeedUserAsync(db, companyId);

        var taskService = new TaskItemService(db, tenant);
        await taskService.CreateAsync(new CreateTaskItemRequest
        {
            Title = "Call buyer",
            AssignedToUserId = userId,
            ReminderAt = DateTime.UtcNow.AddMinutes(5)
        });

        var notificationService = new NoOpNotificationService();
        var job = new ReminderJobs(db, new TenantScope(), notificationService);
        await job.SendDueTaskRemindersAsync();

        var notification = Assert.Single(notificationService.Sent);
        Assert.Equal(userId, notification.UserId);
        Assert.Equal("TaskReminder", notification.Type);
    }

    [Fact]
    public async Task SendDueTaskRemindersAsync_SkipsTasks_NotYetDue()
    {
        var dbName = Guid.NewGuid().ToString();
        var companyId = Guid.NewGuid();
        var tenant = new FakeCurrentTenantService { CompanyId = companyId, UserId = Guid.NewGuid() };
        await using var db = CreateDb(dbName, tenant);
        var userId = await SeedUserAsync(db, companyId);

        var taskService = new TaskItemService(db, tenant);
        await taskService.CreateAsync(new CreateTaskItemRequest
        {
            Title = "Call buyer next week",
            AssignedToUserId = userId,
            ReminderAt = DateTime.UtcNow.AddDays(7)
        });

        var notificationService = new NoOpNotificationService();
        var job = new ReminderJobs(db, new TenantScope(), notificationService);
        await job.SendDueTaskRemindersAsync();

        Assert.Empty(notificationService.Sent);
    }
}
