using RealEstateCRM.Application.Dashboard.DTOs;

namespace RealEstateCRM.Application.Dashboard;

public interface IDashboardService
{
    /// <summary>Cached — see TenantCacheKeys.Dashboard. Short TTL; not explicitly invalidated on writes.</summary>
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
}
