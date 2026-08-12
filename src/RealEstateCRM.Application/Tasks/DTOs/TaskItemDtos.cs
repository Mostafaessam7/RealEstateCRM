using RealEstateCRM.Domain.Enums;

namespace RealEstateCRM.Application.Tasks.DTOs;

public class TaskItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid AssignedToUserId { get; set; }
    public Guid? LeadId { get; set; }
    public Guid? DealId { get; set; }
    public DateTime? DueAt { get; set; }
    public TaskPriority Priority { get; set; }
    public TaskItemStatus Status { get; set; }
    public DateTime? ReminderAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>Never contains CompanyId — that comes from the authenticated context.</summary>
public class CreateTaskItemRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid AssignedToUserId { get; set; }
    public Guid? LeadId { get; set; }
    public Guid? DealId { get; set; }
    public DateTime? DueAt { get; set; }
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public DateTime? ReminderAt { get; set; }
}

/// <summary>Field edits only — status and assignee go through Complete/Cancel/Assign.</summary>
public class UpdateTaskItemRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? LeadId { get; set; }
    public Guid? DealId { get; set; }
    public DateTime? DueAt { get; set; }
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public DateTime? ReminderAt { get; set; }
}

public class AssignTaskItemRequest
{
    public Guid AssignedToUserId { get; set; }
}

public class TaskItemListQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public TaskItemStatus? Status { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public Guid? LeadId { get; set; }
    public Guid? DealId { get; set; }
    public string? SortBy { get; set; }
    public string SortDirection { get; set; } = "asc";
}
