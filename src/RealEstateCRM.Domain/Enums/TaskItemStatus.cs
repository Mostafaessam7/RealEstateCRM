namespace RealEstateCRM.Domain.Enums;

/// <summary>Named TaskItemStatus, not TaskStatus, to avoid colliding with System.Threading.Tasks.TaskStatus.</summary>
public enum TaskItemStatus
{
    Pending,
    Completed,
    Cancelled
}
