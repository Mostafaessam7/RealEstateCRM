using RealEstateCRM.Domain.Common;
using RealEstateCRM.Domain.Enums;

namespace RealEstateCRM.Domain.Entities;

/// <summary>
/// Named TaskItem, not Task, to avoid colliding with System.Threading.Tasks.Task.
/// No soft delete — Cancelled is a terminal status, not a delete.
/// </summary>
public class TaskItem : TenantEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid AssignedToUserId { get; set; }
    public Guid? LeadId { get; set; }
    public Guid? DealId { get; set; }
    public DateTime? DueAt { get; set; }
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public TaskItemStatus Status { get; set; } = TaskItemStatus.Pending;
    public DateTime? ReminderAt { get; set; }

    /// <summary>Set once the task reminder job has notified the assignee. Prevents duplicate reminders.</summary>
    public DateTime? ReminderSentAt { get; set; }
}
