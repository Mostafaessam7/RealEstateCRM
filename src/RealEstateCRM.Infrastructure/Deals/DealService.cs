using Microsoft.EntityFrameworkCore;
using RealEstateCRM.Application.Common.Caching;
using RealEstateCRM.Application.Common.Exceptions;
using RealEstateCRM.Application.Common.Interfaces;
using RealEstateCRM.Application.Common.Models;
using RealEstateCRM.Application.Deals;
using RealEstateCRM.Application.Deals.DTOs;
using RealEstateCRM.Application.Notifications;
using RealEstateCRM.Application.Webhooks;
using RealEstateCRM.Domain.Constants;
using RealEstateCRM.Domain.Entities;
using RealEstateCRM.Domain.Enums;
using RealEstateCRM.Infrastructure.Persistence;
using RealEstateCRM.Infrastructure.Recommendations;
using RealEstateCRM.Infrastructure.Webhooks;

namespace RealEstateCRM.Infrastructure.Deals;

public class DealService : IDealService
{
    private static readonly HashSet<string> SortableFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "CreatedAt", "DealValue", "Status"
    };

    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantService _currentTenant;
    private readonly INotificationService _notificationService;
    private readonly ICacheService _cache;
    private readonly IWebhookPublisher _webhookPublisher;

    public DealService(
        ApplicationDbContext db,
        ICurrentTenantService currentTenant,
        INotificationService notificationService,
        ICacheService cache,
        IWebhookPublisher? webhookPublisher = null)
    {
        _db = db;
        _currentTenant = currentTenant;
        _notificationService = notificationService;
        _cache = cache;
        _webhookPublisher = webhookPublisher ?? NullWebhookPublisher.Instance;
    }

    public async Task<PagedResult<DealDto>> ListAsync(DealListQuery query, CancellationToken cancellationToken = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > 100 ? 20 : query.PageSize;

        var deals = _db.Deals.AsNoTracking().AsQueryable();

        if (query.Status.HasValue)
        {
            deals = deals.Where(d => d.Status == query.Status.Value);
        }

        if (query.LeadId.HasValue)
        {
            deals = deals.Where(d => d.LeadId == query.LeadId.Value);
        }

        if (query.UnitId.HasValue)
        {
            deals = deals.Where(d => d.UnitId == query.UnitId.Value);
        }

        if (query.SalesAgentId.HasValue)
        {
            deals = deals.Where(d => d.SalesAgentId == query.SalesAgentId.Value);
        }

        var sortBy = SortableFields.Contains(query.SortBy ?? string.Empty) ? query.SortBy! : "CreatedAt";
        var descending = !string.Equals(query.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);

        deals = (sortBy, descending) switch
        {
            ("DealValue", true) => deals.OrderByDescending(d => d.DealValue),
            ("DealValue", false) => deals.OrderBy(d => d.DealValue),
            ("Status", true) => deals.OrderByDescending(d => d.Status),
            ("Status", false) => deals.OrderBy(d => d.Status),
            (_, true) => deals.OrderByDescending(d => d.CreatedAt),
            _ => deals.OrderBy(d => d.CreatedAt)
        };

        var totalCount = await deals.CountAsync(cancellationToken);
        var items = await deals.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new PagedResult<DealDto>
        {
            Items = items.Select(ToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<DealDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var deal = await _db.Deals.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, cancellationToken)
            ?? throw new AppException("Deal not found.", 404);

        return ToDto(deal);
    }

    public async Task<DealDto> CreateAsync(CreateDealRequest request, CancellationToken cancellationToken = default)
    {
        var salesAgentId = await ResolveAndValidateAgentAsync(request.SalesAgentId, cancellationToken);

        var lead = await _db.Leads.FirstOrDefaultAsync(l => l.Id == request.LeadId, cancellationToken)
            ?? throw new AppException("Lead not found.", 400);

        var unit = await _db.Units.FirstOrDefaultAsync(u => u.Id == request.UnitId, cancellationToken)
            ?? throw new AppException("Unit not found.", 400);

        if (unit.Status != UnitStatus.Available)
        {
            throw new AppException("Unit is not available.", 409);
        }

        // Captured once, now, so the ML recommendation model (RecommendationService /
        // MlConversionScorer) can later learn from an accurate snapshot of how well this unit
        // matched the lead's preferences at deal time — not however those fields read later.
        var featureSnapshot = LeadUnitFeatureCalculator.Compute(lead, unit);

        var deal = new Deal
        {
            Id = Guid.NewGuid(),
            LeadId = request.LeadId,
            UnitId = request.UnitId,
            SalesAgentId = salesAgentId,
            DealValue = request.DealValue,
            Status = DealStatus.Pending,
            Notes = request.Notes,
            FeatureSnapshotBudgetFit = featureSnapshot.BudgetFit,
            FeatureSnapshotLocationMatch = featureSnapshot.LocationMatch,
            FeatureSnapshotPropertyTypeMatch = featureSnapshot.PropertyTypeMatch,
            FeatureSnapshotPriceToBudgetRatio = featureSnapshot.PriceToBudgetRatio,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Deals.Add(deal);
        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(deal);
    }

    public async Task<DealDto> UpdateAsync(Guid id, UpdateDealRequest request, CancellationToken cancellationToken = default)
    {
        var deal = await GetOwnedDealAsync(id, cancellationToken);

        deal.DealValue = request.DealValue;
        deal.Notes = request.Notes;
        deal.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(deal);
    }

    public async Task<DealDto> ReserveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var deal = await GetOwnedDealAsync(id, cancellationToken);

        if (deal.Status != DealStatus.Pending)
        {
            throw new AppException("Only a Pending deal can be reserved.", 400);
        }

        var unit = await _db.Units.FirstOrDefaultAsync(u => u.Id == deal.UnitId, cancellationToken)
            ?? throw new AppException("Unit not found.", 400);

        if (unit.Status != UnitStatus.Available)
        {
            throw new AppException("Unit is not available.", 409);
        }

        unit.Status = UnitStatus.Reserved;
        unit.UpdatedAt = DateTime.UtcNow;

        deal.Status = DealStatus.Reserved;
        deal.ReservationDate = DateTime.UtcNow;
        deal.UpdatedAt = DateTime.UtcNow;

        await SaveChangesOrThrowConflictAsync(cancellationToken);
        await InvalidateAvailableUnitsCacheAsync(cancellationToken);

        await _notificationService.NotifyUserAsync(
            deal.SalesAgentId, "DealReserved", "Deal reserved", "The unit has been reserved for this deal.", cancellationToken);

        return ToDto(deal);
    }

    public async Task<DealDto> ContractAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var deal = await GetOwnedDealAsync(id, cancellationToken);

        if (deal.Status != DealStatus.Reserved)
        {
            throw new AppException("Only a Reserved deal can be contracted.", 400);
        }

        var unit = await _db.Units.FirstOrDefaultAsync(u => u.Id == deal.UnitId, cancellationToken)
            ?? throw new AppException("Unit not found.", 400);

        unit.Status = UnitStatus.Sold;
        unit.UpdatedAt = DateTime.UtcNow;

        deal.Status = DealStatus.Contracted;
        deal.ContractDate = DateTime.UtcNow;
        deal.UpdatedAt = DateTime.UtcNow;

        await SaveChangesOrThrowConflictAsync(cancellationToken);
        await InvalidateAvailableUnitsCacheAsync(cancellationToken);

        await _notificationService.NotifyUserAsync(
            deal.SalesAgentId, "DealContracted", "Deal contracted", "This deal has been contracted.", cancellationToken);

        await _webhookPublisher.PublishAsync(WebhookEventTypes.DealContracted, ToDto(deal), cancellationToken);

        return ToDto(deal);
    }

    public async Task<DealDto> CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var deal = await GetOwnedDealAsync(id, cancellationToken);

        if (deal.Status is DealStatus.Contracted or DealStatus.Cancelled)
        {
            throw new AppException($"A deal in {deal.Status} status cannot be cancelled.", 400);
        }

        var revertedUnitToAvailable = false;

        if (deal.Status == DealStatus.Reserved)
        {
            var unit = await _db.Units.FirstOrDefaultAsync(u => u.Id == deal.UnitId, cancellationToken);
            if (unit is not null && unit.Status == UnitStatus.Reserved)
            {
                unit.Status = UnitStatus.Available;
                unit.UpdatedAt = DateTime.UtcNow;
                revertedUnitToAvailable = true;
            }
        }

        deal.Status = DealStatus.Cancelled;
        deal.UpdatedAt = DateTime.UtcNow;

        await SaveChangesOrThrowConflictAsync(cancellationToken);

        if (revertedUnitToAvailable)
        {
            await InvalidateAvailableUnitsCacheAsync(cancellationToken);
        }

        await _notificationService.NotifyUserAsync(
            deal.SalesAgentId, "DealCancelled", "Deal cancelled", "This deal has been cancelled.", cancellationToken);

        return ToDto(deal);
    }

    /// <summary>Loads the deal and enforces Deal authorization: elevated roles manage any deal, everyone else only their own.</summary>
    private async Task<Deal> GetOwnedDealAsync(Guid id, CancellationToken cancellationToken)
    {
        var deal = await _db.Deals.FirstOrDefaultAsync(d => d.Id == id, cancellationToken)
            ?? throw new AppException("Deal not found.", 404);

        if (!CanManage(deal))
        {
            throw new AppException("You are not authorized to manage this deal.", 403);
        }

        return deal;
    }

    private bool CanManage(Deal deal)
    {
        if (HasElevatedAccess())
        {
            return true;
        }

        return _currentTenant.UserId.HasValue && _currentTenant.UserId.Value == deal.SalesAgentId;
    }

    private bool HasElevatedAccess() =>
        _currentTenant.IsSuperAdmin ||
        _currentTenant.IsInRole(Roles.CompanyAdmin) ||
        _currentTenant.IsInRole(Roles.SalesManager);

    /// <summary>
    /// Unit.UpdatedAt is an optimistic concurrency token (see UnitConfiguration) specifically to
    /// catch two agents racing to Reserve/Contract/Cancel against the same unit at once — e.g.
    /// two concurrent ReserveAsync calls both reading Status == Available before either writes.
    /// The loser's SaveChangesAsync throws DbUpdateConcurrencyException instead of silently
    /// overwriting the winner; translate that into a normal 409 the caller already knows to
    /// handle (the same status code already used for "unit is not available").
    /// </summary>
    private async Task SaveChangesOrThrowConflictAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AppException("This unit was just updated by someone else. Please refresh and try again.", 409);
        }
    }

    private Task InvalidateAvailableUnitsCacheAsync(CancellationToken cancellationToken)
    {
        var companyId = _currentTenant.CompanyId;
        return companyId.HasValue
            ? _cache.RemoveAsync(TenantCacheKeys.AvailableUnits(companyId.Value), cancellationToken)
            : Task.CompletedTask;
    }

    private async Task<Guid> ResolveAndValidateAgentAsync(Guid? requestedAgentId, CancellationToken cancellationToken)
    {
        Guid salesAgentId;

        if (requestedAgentId.HasValue)
        {
            if (!HasElevatedAccess() && requestedAgentId.Value != _currentTenant.UserId)
            {
                throw new AppException("You may only create deals assigned to yourself.", 403);
            }

            salesAgentId = requestedAgentId.Value;
        }
        else
        {
            salesAgentId = _currentTenant.UserId
                ?? throw new AppException("Authenticated user context is required.", 401);
        }

        var exists = await _db.Users.AnyAsync(
            u => u.Id == salesAgentId && u.CompanyId == _currentTenant.CompanyId,
            cancellationToken);

        if (!exists)
        {
            throw new AppException("Sales agent not found in this company.", 400);
        }

        return salesAgentId;
    }

    private static DealDto ToDto(Deal deal) => new()
    {
        Id = deal.Id,
        LeadId = deal.LeadId,
        UnitId = deal.UnitId,
        SalesAgentId = deal.SalesAgentId,
        DealValue = deal.DealValue,
        Status = deal.Status,
        ReservationDate = deal.ReservationDate,
        ContractDate = deal.ContractDate,
        Notes = deal.Notes,
        CreatedAt = deal.CreatedAt,
        UpdatedAt = deal.UpdatedAt
    };
}
