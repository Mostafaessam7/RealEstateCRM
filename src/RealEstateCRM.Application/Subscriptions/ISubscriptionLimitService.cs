namespace RealEstateCRM.Application.Subscriptions;

/// <summary>
/// Enforces the current company's plan limits at the point a resource is created. Throws
/// AppException(402) when the limit is reached. A company with no provisioned subscription
/// yet is allowed through (GetCurrentAsync lazily provisions a Free trial on first billing
/// access) so this never blocks flows that never touch the billing page.
/// </summary>
public interface ISubscriptionLimitService
{
    Task EnsureCanAddUserAsync(CancellationToken cancellationToken = default);
    Task EnsureCanAddLeadAsync(CancellationToken cancellationToken = default);
    Task EnsureCanAddUnitAsync(CancellationToken cancellationToken = default);
}
