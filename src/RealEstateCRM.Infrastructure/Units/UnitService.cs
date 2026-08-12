using Microsoft.EntityFrameworkCore;
using RealEstateCRM.Application.Common.Caching;
using RealEstateCRM.Application.Common.Exceptions;
using RealEstateCRM.Application.Common.Interfaces;
using RealEstateCRM.Application.Common.Models;
using RealEstateCRM.Application.Subscriptions;
using RealEstateCRM.Application.Units;
using RealEstateCRM.Application.Units.DTOs;
using RealEstateCRM.Domain.Entities;
using RealEstateCRM.Domain.Enums;
using RealEstateCRM.Infrastructure.Persistence;
using RealEstateCRM.Infrastructure.Subscriptions;

namespace RealEstateCRM.Infrastructure.Units;

public class UnitService : IUnitService
{
    private static readonly TimeSpan AvailableUnitsCacheTtl = TimeSpan.FromMinutes(2);

    private static readonly HashSet<string> SortableFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "CreatedAt", "Price", "UnitCode", "Status"
    };

    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantService _currentTenant;
    private readonly ICacheService _cache;
    private readonly ISubscriptionLimitService _subscriptionLimit;

    public UnitService(
        ApplicationDbContext db,
        ICurrentTenantService currentTenant,
        ICacheService cache,
        ISubscriptionLimitService? subscriptionLimit = null)
    {
        _db = db;
        _currentTenant = currentTenant;
        _cache = cache;
        _subscriptionLimit = subscriptionLimit ?? NullSubscriptionLimitService.Instance;
    }

    public async Task<PagedResult<UnitDto>> ListAsync(UnitListQuery query, CancellationToken cancellationToken = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > 100 ? 20 : query.PageSize;

        var units = _db.Units.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            units = units.Where(u =>
                EF.Functions.Like(u.UnitCode, $"%{term}%") ||
                (u.Location != null && EF.Functions.Like(u.Location, $"%{term}%")) ||
                _db.Projects.Any(p => p.Id == u.ProjectId && EF.Functions.Like(p.Name, $"%{term}%")));
        }

        if (query.Status.HasValue)
        {
            units = units.Where(u => u.Status == query.Status.Value);
        }

        if (query.ProjectId.HasValue)
        {
            units = units.Where(u => u.ProjectId == query.ProjectId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.PropertyType))
        {
            units = units.Where(u => u.PropertyType == query.PropertyType);
        }

        var sortBy = SortableFields.Contains(query.SortBy ?? string.Empty) ? query.SortBy! : "CreatedAt";
        var descending = !string.Equals(query.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);

        units = (sortBy, descending) switch
        {
            ("Price", true) => units.OrderByDescending(u => u.Price),
            ("Price", false) => units.OrderBy(u => u.Price),
            ("UnitCode", true) => units.OrderByDescending(u => u.UnitCode),
            ("UnitCode", false) => units.OrderBy(u => u.UnitCode),
            ("Status", true) => units.OrderByDescending(u => u.Status),
            ("Status", false) => units.OrderBy(u => u.Status),
            (_, true) => units.OrderByDescending(u => u.CreatedAt),
            _ => units.OrderBy(u => u.CreatedAt)
        };

        var totalCount = await units.CountAsync(cancellationToken);
        var items = await units.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new PagedResult<UnitDto>
        {
            Items = items.Select(ToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<UnitDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var unit = await _db.Units.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
            ?? throw new AppException("Unit not found.", 404);

        return ToDto(unit);
    }

    public async Task<UnitDto> CreateAsync(CreateUnitRequest request, CancellationToken cancellationToken = default)
    {
        await _subscriptionLimit.EnsureCanAddUnitAsync(cancellationToken);
        await EnsureProjectExistsAsync(request.ProjectId, cancellationToken);
        await EnsureUnitCodeAvailableAsync(request.ProjectId, request.UnitCode, excludeUnitId: null, cancellationToken);

        var unit = new Unit
        {
            Id = Guid.NewGuid(),
            ProjectId = request.ProjectId,
            UnitCode = request.UnitCode.Trim(),
            PropertyType = request.PropertyType,
            Price = request.Price,
            Area = request.Area,
            Bedrooms = request.Bedrooms,
            Bathrooms = request.Bathrooms,
            Floor = request.Floor,
            Location = request.Location,
            Status = request.Status,
            DownPayment = request.DownPayment,
            InstallmentYears = request.InstallmentYears,
            Description = request.Description,
            IsPubliclyListed = request.IsPubliclyListed,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Units.Add(unit);
        await SaveWithUniqueUnitCodeCheckAsync(cancellationToken);
        await InvalidateAvailableUnitsCacheAsync(cancellationToken);

        return ToDto(unit);
    }

    public async Task<UnitDto> UpdateAsync(Guid id, UpdateUnitRequest request, CancellationToken cancellationToken = default)
    {
        var unit = await _db.Units.FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
            ?? throw new AppException("Unit not found.", 404);

        await EnsureProjectExistsAsync(request.ProjectId, cancellationToken);
        await EnsureUnitCodeAvailableAsync(request.ProjectId, request.UnitCode, excludeUnitId: id, cancellationToken);

        unit.ProjectId = request.ProjectId;
        unit.UnitCode = request.UnitCode.Trim();
        unit.PropertyType = request.PropertyType;
        unit.Price = request.Price;
        unit.Area = request.Area;
        unit.Bedrooms = request.Bedrooms;
        unit.Bathrooms = request.Bathrooms;
        unit.Floor = request.Floor;
        unit.Location = request.Location;
        unit.Status = request.Status;
        unit.DownPayment = request.DownPayment;
        unit.InstallmentYears = request.InstallmentYears;
        unit.Description = request.Description;
        unit.IsPubliclyListed = request.IsPubliclyListed;
        unit.UpdatedAt = DateTime.UtcNow;

        await SaveWithUniqueUnitCodeCheckAsync(cancellationToken);
        await InvalidateAvailableUnitsCacheAsync(cancellationToken);

        return ToDto(unit);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var unit = await _db.Units.FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
            ?? throw new AppException("Unit not found.", 404);

        unit.IsDeleted = true;
        unit.DeletedAt = DateTime.UtcNow;
        unit.DeletedBy = _currentTenant.UserId;
        unit.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        await InvalidateAvailableUnitsCacheAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UnitDto>> GetAvailableAsync(Guid? projectId, CancellationToken cancellationToken = default)
    {
        // Only the unfiltered "all available units" query is cached — it's the one named
        // explicitly in docs/multi-tenancy.md#redis (tenant:{companyId}:units:available).
        // Per-project results are cheap enough to query directly and keep invalidation simple.
        if (projectId.HasValue)
        {
            return await QueryAvailableAsync(projectId, cancellationToken);
        }

        var companyId = _currentTenant.CompanyId
            ?? throw new AppException("Authenticated company context is required.", 401);

        return await _cache.GetOrCreateAsync(
            TenantCacheKeys.AvailableUnits(companyId),
            ct => QueryAvailableAsync(null, ct),
            AvailableUnitsCacheTtl,
            cancellationToken);
    }

    private async Task<IReadOnlyList<UnitDto>> QueryAvailableAsync(Guid? projectId, CancellationToken cancellationToken)
    {
        var units = _db.Units.AsNoTracking().Where(u => u.Status == UnitStatus.Available);

        if (projectId.HasValue)
        {
            units = units.Where(u => u.ProjectId == projectId.Value);
        }

        var results = await units.OrderBy(u => u.UnitCode).ToListAsync(cancellationToken);
        return results.Select(ToDto).ToList();
    }

    private Task InvalidateAvailableUnitsCacheAsync(CancellationToken cancellationToken)
    {
        var companyId = _currentTenant.CompanyId;
        return companyId.HasValue
            ? _cache.RemoveAsync(TenantCacheKeys.AvailableUnits(companyId.Value), cancellationToken)
            : Task.CompletedTask;
    }

    private async Task EnsureProjectExistsAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var exists = await _db.Projects.AnyAsync(p => p.Id == projectId, cancellationToken);
        if (!exists)
        {
            throw new AppException("Project not found in this company.", 400);
        }
    }

    private async Task EnsureUnitCodeAvailableAsync(Guid projectId, string unitCode, Guid? excludeUnitId, CancellationToken cancellationToken)
    {
        var code = unitCode.Trim();
        var duplicate = await _db.Units.AnyAsync(
            u => u.ProjectId == projectId && u.UnitCode == code && u.Id != excludeUnitId,
            cancellationToken);

        if (duplicate)
        {
            throw new AppException("A unit with this code already exists in this project.", 409);
        }
    }

    private async Task SaveWithUniqueUnitCodeCheckAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // DbUpdateConcurrencyException derives from DbUpdateException — this catch must come
            // first, or a real concurrent-edit conflict (see UnitConfiguration's UpdatedAt
            // concurrency token) gets mislabeled as a duplicate unit code below.
            throw new AppException("This unit was just updated by someone else. Please refresh and try again.", 409);
        }
        catch (DbUpdateException)
        {
            throw new AppException("A unit with this code already exists in this project.", 409);
        }
    }

    private static UnitDto ToDto(Unit unit) => new()
    {
        Id = unit.Id,
        ProjectId = unit.ProjectId,
        UnitCode = unit.UnitCode,
        PropertyType = unit.PropertyType,
        Price = unit.Price,
        Area = unit.Area,
        Bedrooms = unit.Bedrooms,
        Bathrooms = unit.Bathrooms,
        Floor = unit.Floor,
        Location = unit.Location,
        Status = unit.Status,
        DownPayment = unit.DownPayment,
        InstallmentYears = unit.InstallmentYears,
        Description = unit.Description,
        IsPubliclyListed = unit.IsPubliclyListed,
        CreatedAt = unit.CreatedAt,
        UpdatedAt = unit.UpdatedAt
    };
}
