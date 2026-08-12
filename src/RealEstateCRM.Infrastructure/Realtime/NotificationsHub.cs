using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace RealEstateCRM.Infrastructure.Realtime;

/// <summary>
/// Every connection joins its tenant's group on connect so Clients.Group("tenant:{companyId}")
/// can broadcast tenant-wide, and the default SignalR user-id provider (backed by the JWT's
/// NameIdentifier claim) lets Clients.User(userId) target one person directly.
/// Never send raw business data to the whole hub — only to a specific user or tenant group.
/// See docs/multi-tenancy.md#signalr.
/// </summary>
[Authorize]
public class NotificationsHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var companyId = Context.User?.FindFirst("company_id")?.Value;
        if (!string.IsNullOrEmpty(companyId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, TenantGroupName(companyId));
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var companyId = Context.User?.FindFirst("company_id")?.Value;
        if (!string.IsNullOrEmpty(companyId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, TenantGroupName(companyId));
        }

        await base.OnDisconnectedAsync(exception);
    }

    public static string TenantGroupName(string companyId) => $"tenant:{companyId}";
    public static string TenantGroupName(Guid companyId) => TenantGroupName(companyId.ToString());
}
