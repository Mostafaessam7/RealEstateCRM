using Microsoft.AspNetCore.SignalR;
using RealEstateCRM.Application.Notifications;
using RealEstateCRM.Domain.Entities;
using RealEstateCRM.Infrastructure.Persistence;
using RealEstateCRM.Infrastructure.Realtime;

namespace RealEstateCRM.Infrastructure.Notifications;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _db;
    private readonly IHubContext<NotificationsHub> _hub;

    public NotificationService(ApplicationDbContext db, IHubContext<NotificationsHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    public async Task NotifyUserAsync(Guid userId, string type, string title, string message, CancellationToken cancellationToken = default)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            CreatedAt = DateTime.UtcNow
        };

        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync(cancellationToken);

        await _hub.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", new
        {
            notification.Id,
            notification.Type,
            notification.Title,
            notification.Message,
            notification.CreatedAt
        }, cancellationToken);
    }

    public Task NotifyTenantAsync(Guid companyId, string type, string title, string message, CancellationToken cancellationToken = default)
    {
        return _hub.Clients.Group(NotificationsHub.TenantGroupName(companyId)).SendAsync("ReceiveNotification", new
        {
            Type = type,
            Title = title,
            Message = message,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);
    }
}
