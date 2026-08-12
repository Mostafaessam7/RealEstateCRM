using Microsoft.EntityFrameworkCore;
using RealEstateCRM.Application.Common.Caching;
using RealEstateCRM.Application.Common.Exceptions;
using RealEstateCRM.Application.Common.Interfaces;
using RealEstateCRM.Application.Dashboard;
using RealEstateCRM.Application.Dashboard.DTOs;
using RealEstateCRM.Domain.Enums;
using RealEstateCRM.Infrastructure.Persistence;

namespace RealEstateCRM.Infrastructure.Dashboard;

public class DashboardService : IDashboardService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(1);

    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantService _currentTenant;
    private readonly ICacheService _cache;

    public DashboardService(ApplicationDbContext db, ICurrentTenantService currentTenant, ICacheService cache)
    {
        _db = db;
        _currentTenant = currentTenant;
        _cache = cache;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var companyId = _currentTenant.CompanyId
            ?? throw new AppException("Authenticated company context is required.", 401);

        return await _cache.GetOrCreateAsync(
            TenantCacheKeys.Dashboard(companyId),
            async ct =>
            {
                var now = DateTime.UtcNow;
                var thirtyDaysAgo = now.AddDays(-30);
                var followUpWindow = now.AddDays(7);

                var totalLeads = await _db.Leads.CountAsync(ct);
                var contractedLeads = await _db.Leads.CountAsync(l => l.Status == LeadStatus.Contracted, ct);
                var newLeads = await _db.Leads.CountAsync(l => l.CreatedAt >= thirtyDaysAgo, ct);
                var totalDeals = await _db.Deals.CountAsync(ct);
                var activeDeals = await _db.Deals.CountAsync(d => d.Status == DealStatus.Pending || d.Status == DealStatus.Reserved, ct);
                var totalSalesValue = await _db.Deals
                    .Where(d => d.Status == DealStatus.Contracted)
                    .SumAsync(d => (decimal?)d.DealValue, ct) ?? 0m;
                var upcomingFollowUps = await _db.LeadActivities.CountAsync(
                    a => a.Type == LeadActivityType.FollowUp && a.ActivityDate >= now && a.ActivityDate <= followUpWindow, ct);
                var availableUnits = await _db.Units.CountAsync(u => u.Status == UnitStatus.Available, ct);

                return new DashboardSummaryDto
                {
                    TotalLeads = totalLeads,
                    NewLeadsLast30Days = newLeads,
                    ConversionRatePercent = totalLeads == 0 ? 0 : Math.Round(contractedLeads * 100.0 / totalLeads, 1),
                    TotalDeals = totalDeals,
                    TotalActiveDeals = activeDeals,
                    TotalSalesValue = totalSalesValue,
                    UpcomingFollowUps = upcomingFollowUps,
                    TotalAvailableUnits = availableUnits
                };
            },
            CacheTtl,
            cancellationToken);
    }
}
