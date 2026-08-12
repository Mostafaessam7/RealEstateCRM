using Microsoft.EntityFrameworkCore;
using RealEstateCRM.Application.Common.Exceptions;
using RealEstateCRM.Application.Common.Interfaces;
using RealEstateCRM.Application.Subscriptions;
using RealEstateCRM.Domain.Entities;
using RealEstateCRM.Domain.Enums;
using RealEstateCRM.Infrastructure.Persistence;

namespace RealEstateCRM.Infrastructure.Subscriptions;

public class SubscriptionLimitService : ISubscriptionLimitService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantService _currentTenant;

    public SubscriptionLimitService(ApplicationDbContext db, ICurrentTenantService currentTenant)
    {
        _db = db;
        _currentTenant = currentTenant;
    }

    public Task EnsureCanAddUserAsync(CancellationToken cancellationToken = default) =>
        EnsureWithinLimitAsync(p => p.MaxUsers, companyId => _db.Users.CountAsync(u => u.CompanyId == companyId, cancellationToken), "users", cancellationToken);

    public Task EnsureCanAddLeadAsync(CancellationToken cancellationToken = default) =>
        EnsureWithinLimitAsync(p => p.MaxLeads, companyId => _db.Leads.CountAsync(l => l.CompanyId == companyId, cancellationToken), "leads", cancellationToken);

    public Task EnsureCanAddUnitAsync(CancellationToken cancellationToken = default) =>
        EnsureWithinLimitAsync(p => p.MaxUnits, companyId => _db.Units.CountAsync(u => u.CompanyId == companyId, cancellationToken), "units", cancellationToken);

    private async Task EnsureWithinLimitAsync(
        Func<SubscriptionPlan, int> selectLimit,
        Func<Guid, Task<int>> countCurrent,
        string resourceName,
        CancellationToken cancellationToken)
    {
        var companyId = _currentTenant.CompanyId;
        if (companyId is null)
        {
            return;
        }

        var subscription = await _db.CompanySubscriptions.AsNoTracking()
            .FirstOrDefaultAsync(s => s.CompanyId == companyId.Value, cancellationToken);
        if (subscription is null)
        {
            // Not yet provisioned (only happens on first /subscriptions/current access) — allow.
            return;
        }

        if (subscription.Status == SubscriptionStatus.Cancelled)
        {
            throw new AppException($"Your subscription is cancelled — reactivate a plan to add more {resourceName}.", 402);
        }

        var plan = await _db.SubscriptionPlans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == subscription.PlanId, cancellationToken);
        if (plan is null)
        {
            return;
        }

        var limit = selectLimit(plan);
        var current = await countCurrent(companyId.Value);

        if (current >= limit)
        {
            throw new AppException($"Your {plan.Name} plan allows up to {limit} {resourceName}. Upgrade your plan to add more.", 402);
        }
    }
}
