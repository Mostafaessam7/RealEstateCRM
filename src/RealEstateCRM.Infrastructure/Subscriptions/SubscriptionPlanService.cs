using Microsoft.EntityFrameworkCore;
using RealEstateCRM.Application.Common.Exceptions;
using RealEstateCRM.Application.Common.Interfaces;
using RealEstateCRM.Application.Subscriptions;
using RealEstateCRM.Application.Subscriptions.DTOs;
using RealEstateCRM.Domain.Entities;
using RealEstateCRM.Infrastructure.Persistence;

namespace RealEstateCRM.Infrastructure.Subscriptions;

public class SubscriptionPlanService : ISubscriptionPlanService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantService _currentTenant;

    public SubscriptionPlanService(ApplicationDbContext db, ICurrentTenantService currentTenant)
    {
        _db = db;
        _currentTenant = currentTenant;
    }

    public async Task<IReadOnlyList<SubscriptionPlanDto>> ListAsync(bool activeOnly, CancellationToken cancellationToken = default)
    {
        var plans = _db.SubscriptionPlans.AsNoTracking().AsQueryable();
        if (activeOnly)
        {
            plans = plans.Where(p => p.IsActive);
        }

        var items = await plans.OrderBy(p => p.MonthlyPrice).ToListAsync(cancellationToken);
        return items.Select(ToDto).ToList();
    }

    public async Task<SubscriptionPlanDto> CreateAsync(CreateSubscriptionPlanRequest request, CancellationToken cancellationToken = default)
    {
        EnsureSuperAdmin();

        var codeExists = await _db.SubscriptionPlans.AnyAsync(p => p.Code == request.Code, cancellationToken);
        if (codeExists)
        {
            throw new AppException("A plan with this code already exists.", 409);
        }

        var plan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Code = request.Code,
            Name = request.Name,
            MonthlyPrice = request.MonthlyPrice,
            MaxUsers = request.MaxUsers,
            MaxLeads = request.MaxLeads,
            MaxUnits = request.MaxUnits,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.SubscriptionPlans.Add(plan);
        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(plan);
    }

    public async Task<SubscriptionPlanDto> UpdateAsync(Guid id, UpdateSubscriptionPlanRequest request, CancellationToken cancellationToken = default)
    {
        EnsureSuperAdmin();

        var plan = await _db.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new AppException("Plan not found.", 404);

        plan.Name = request.Name;
        plan.MonthlyPrice = request.MonthlyPrice;
        plan.MaxUsers = request.MaxUsers;
        plan.MaxLeads = request.MaxLeads;
        plan.MaxUnits = request.MaxUnits;
        plan.IsActive = request.IsActive;
        plan.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(plan);
    }

    private void EnsureSuperAdmin()
    {
        if (!_currentTenant.IsSuperAdmin)
        {
            throw new AppException("Only a platform administrator can manage subscription plans.", 403);
        }
    }

    private static SubscriptionPlanDto ToDto(SubscriptionPlan plan) => new()
    {
        Id = plan.Id,
        Code = plan.Code,
        Name = plan.Name,
        MonthlyPrice = plan.MonthlyPrice,
        MaxUsers = plan.MaxUsers,
        MaxLeads = plan.MaxLeads,
        MaxUnits = plan.MaxUnits,
        IsActive = plan.IsActive
    };
}
