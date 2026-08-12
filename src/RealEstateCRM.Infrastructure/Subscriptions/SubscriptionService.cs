using Microsoft.EntityFrameworkCore;
using RealEstateCRM.Application.Common.Exceptions;
using RealEstateCRM.Application.Common.Interfaces;
using RealEstateCRM.Application.Subscriptions;
using RealEstateCRM.Application.Subscriptions.DTOs;
using RealEstateCRM.Domain.Constants;
using RealEstateCRM.Domain.Entities;
using RealEstateCRM.Domain.Enums;
using RealEstateCRM.Infrastructure.Persistence;

namespace RealEstateCRM.Infrastructure.Subscriptions;

public class SubscriptionService : ISubscriptionService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantService _currentTenant;

    public SubscriptionService(ApplicationDbContext db, ICurrentTenantService currentTenant)
    {
        _db = db;
        _currentTenant = currentTenant;
    }

    public async Task<CompanySubscriptionDto> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        var companyId = RequireCompanyId();
        var subscription = await GetOrCreateSubscriptionAsync(companyId, cancellationToken);
        return await ToDtoAsync(subscription, cancellationToken);
    }

    public async Task<CompanySubscriptionDto> ChangePlanAsync(ChangePlanRequest request, CancellationToken cancellationToken = default)
    {
        EnsureElevatedAccess();
        var companyId = RequireCompanyId();

        var plan = await _db.SubscriptionPlans.FirstOrDefaultAsync(p => p.Code == request.PlanCode && p.IsActive, cancellationToken)
            ?? throw new AppException("Plan not found or inactive.", 404);

        var subscription = await GetOrCreateSubscriptionAsync(companyId, cancellationToken);

        subscription.PlanId = plan.Id;
        subscription.Status = SubscriptionStatus.Active;
        subscription.CurrentPeriodStart = DateTime.UtcNow;
        subscription.CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1);
        subscription.CancelledAt = null;
        subscription.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return await ToDtoAsync(subscription, cancellationToken);
    }

    public async Task<CompanySubscriptionDto> CancelAsync(CancellationToken cancellationToken = default)
    {
        EnsureElevatedAccess();
        var companyId = RequireCompanyId();

        var subscription = await GetOrCreateSubscriptionAsync(companyId, cancellationToken);

        if (subscription.Status == SubscriptionStatus.Cancelled)
        {
            throw new AppException("Subscription is already cancelled.", 400);
        }

        subscription.Status = SubscriptionStatus.Cancelled;
        subscription.CancelledAt = DateTime.UtcNow;
        subscription.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return await ToDtoAsync(subscription, cancellationToken);
    }

    private async Task<CompanySubscription> GetOrCreateSubscriptionAsync(Guid companyId, CancellationToken cancellationToken)
    {
        var subscription = await _db.CompanySubscriptions.FirstOrDefaultAsync(s => s.CompanyId == companyId, cancellationToken);
        if (subscription is not null)
        {
            return subscription;
        }

        var freePlan = await _db.SubscriptionPlans.FirstOrDefaultAsync(p => p.Code == "free", cancellationToken)
            ?? throw new AppException("Default plan is not configured.", 500);

        var now = DateTime.UtcNow;
        subscription = new CompanySubscription
        {
            Id = Guid.NewGuid(),
            PlanId = freePlan.Id,
            Status = SubscriptionStatus.Trialing,
            TrialEndsAt = now.AddDays(14),
            CurrentPeriodStart = now,
            CurrentPeriodEnd = now.AddDays(14),
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.CompanySubscriptions.Add(subscription);
        await _db.SaveChangesAsync(cancellationToken);

        return subscription;
    }

    private async Task<CompanySubscriptionDto> ToDtoAsync(CompanySubscription subscription, CancellationToken cancellationToken)
    {
        var plan = await _db.SubscriptionPlans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == subscription.PlanId, cancellationToken)
            ?? throw new AppException("Plan not found.", 500);

        var userCount = await _db.Users.CountAsync(u => u.CompanyId == subscription.CompanyId, cancellationToken);
        var leadCount = await _db.Leads.CountAsync(l => l.CompanyId == subscription.CompanyId, cancellationToken);
        var unitCount = await _db.Units.CountAsync(u => u.CompanyId == subscription.CompanyId, cancellationToken);

        return new CompanySubscriptionDto
        {
            Id = subscription.Id,
            Plan = new SubscriptionPlanDto
            {
                Id = plan.Id,
                Code = plan.Code,
                Name = plan.Name,
                MonthlyPrice = plan.MonthlyPrice,
                MaxUsers = plan.MaxUsers,
                MaxLeads = plan.MaxLeads,
                MaxUnits = plan.MaxUnits,
                IsActive = plan.IsActive
            },
            Status = subscription.Status,
            TrialEndsAt = subscription.TrialEndsAt,
            CurrentPeriodStart = subscription.CurrentPeriodStart,
            CurrentPeriodEnd = subscription.CurrentPeriodEnd,
            CancelledAt = subscription.CancelledAt,
            Usage = new SubscriptionUsageDto
            {
                UserCount = userCount,
                LeadCount = leadCount,
                UnitCount = unitCount
            }
        };
    }

    private Guid RequireCompanyId() =>
        _currentTenant.CompanyId ?? throw new AppException("Authenticated company context is required.", 401);

    private void EnsureElevatedAccess()
    {
        var allowed = _currentTenant.IsSuperAdmin || _currentTenant.IsInRole(Roles.CompanyAdmin);
        if (!allowed)
        {
            throw new AppException("You are not authorized to manage the subscription.", 403);
        }
    }
}
