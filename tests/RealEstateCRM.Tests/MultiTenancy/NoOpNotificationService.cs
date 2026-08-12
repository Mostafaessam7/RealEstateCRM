using RealEstateCRM.Application.Notifications;

namespace RealEstateCRM.Tests.MultiTenancy;

/// <summary>
/// Test double — real NotificationService needs a live SignalR IHubContext. Service-level
/// tests only care that the business flow completed, not that a push actually went out.
/// </summary>
internal class NoOpNotificationService : INotificationService
{
    public List<(Guid UserId, string Type, string Title, string Message)> Sent { get; } = new();

    public Task NotifyUserAsync(Guid userId, string type, string title, string message, CancellationToken cancellationToken = default)
    {
        Sent.Add((userId, type, title, message));
        return Task.CompletedTask;
    }

    public Task NotifyTenantAsync(Guid companyId, string type, string title, string message, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
