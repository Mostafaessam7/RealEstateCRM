namespace RealEstateCRM.Application.Notifications;

public interface INotificationService
{
    /// <summary>Persists a Notification row and pushes it live to that user's SignalR connections.</summary>
    Task NotifyUserAsync(Guid userId, string type, string title, string message, CancellationToken cancellationToken = default);

    /// <summary>Live broadcast to every connection in the tenant's SignalR group. Not persisted per-user.</summary>
    Task NotifyTenantAsync(Guid companyId, string type, string title, string message, CancellationToken cancellationToken = default);
}
