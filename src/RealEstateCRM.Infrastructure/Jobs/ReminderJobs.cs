using Hangfire;
using Microsoft.EntityFrameworkCore;
using RealEstateCRM.Application.Common.Interfaces;
using RealEstateCRM.Application.Notifications;
using RealEstateCRM.Domain.Entities;
using RealEstateCRM.Domain.Enums;
using RealEstateCRM.Infrastructure.Persistence;

namespace RealEstateCRM.Infrastructure.Jobs;

/// <summary>
/// Recurring jobs with no HTTP context. Each processes every tenant, but explicitly opens
/// an ITenantScope per company before writing — see docs/multi-tenancy.md#background-jobs.
/// Failed executions are retried automatically by Hangfire (AutomaticRetry) and land in the
/// Hangfire dashboard's Failed jobs list if retries are exhausted.
/// </summary>
public class ReminderJobs
{
    private const int ReminderWindowMinutes = 15;

    private readonly ApplicationDbContext _db;
    private readonly ITenantScope _tenantScope;
    private readonly INotificationService _notificationService;

    public ReminderJobs(ApplicationDbContext db, ITenantScope tenantScope, INotificationService notificationService)
    {
        _db = db;
        _tenantScope = tenantScope;
        _notificationService = notificationService;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task SendDueFollowUpRemindersAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var window = now.AddMinutes(ReminderWindowMinutes);

        var dueActivities = await _db.ForAllTenants<LeadActivity>()
            .Where(a => a.Type == LeadActivityType.FollowUp && a.ReminderSentAt == null && a.ActivityDate <= window)
            .ToListAsync(cancellationToken);

        foreach (var group in dueActivities.GroupBy(a => a.CompanyId))
        {
            using var scope = _tenantScope.Begin(group.Key);

            var leadIds = group.Select(a => a.LeadId).ToHashSet();
            // ForAllTenants bypasses the combined tenant+soft-delete filter — exclude soft-deleted leads explicitly.
            var leads = await _db.ForAllTenants<Lead>()
                .Where(l => leadIds.Contains(l.Id) && !l.IsDeleted)
                .ToDictionaryAsync(l => l.Id, cancellationToken);

            foreach (var activity in group)
            {
                activity.ReminderSentAt = now;

                if (leads.TryGetValue(activity.LeadId, out var lead) && lead.AssignedAgentId.HasValue)
                {
                    await _notificationService.NotifyUserAsync(
                        lead.AssignedAgentId.Value,
                        "FollowUpReminder",
                        "Follow-up due",
                        $"Follow-up with {lead.FullName} is due.",
                        cancellationToken);
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task SendDueTaskRemindersAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var window = now.AddMinutes(ReminderWindowMinutes);

        var dueTasks = await _db.ForAllTenants<TaskItem>()
            .Where(t => t.Status == TaskItemStatus.Pending
                && t.ReminderSentAt == null
                && t.ReminderAt != null
                && t.ReminderAt <= window)
            .ToListAsync(cancellationToken);

        foreach (var group in dueTasks.GroupBy(t => t.CompanyId))
        {
            using var scope = _tenantScope.Begin(group.Key);

            foreach (var task in group)
            {
                task.ReminderSentAt = now;

                await _notificationService.NotifyUserAsync(
                    task.AssignedToUserId,
                    "TaskReminder",
                    "Task due",
                    $"Task \"{task.Title}\" is due.",
                    cancellationToken);
            }

            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
