using Microsoft.EntityFrameworkCore;
using RealEstateCRM.Application.Commissions;
using RealEstateCRM.Application.Commissions.DTOs;
using RealEstateCRM.Application.Common.Exceptions;
using RealEstateCRM.Application.Common.Interfaces;
using RealEstateCRM.Application.Common.Models;
using RealEstateCRM.Domain.Constants;
using RealEstateCRM.Domain.Entities;
using RealEstateCRM.Domain.Enums;
using RealEstateCRM.Infrastructure.Persistence;

namespace RealEstateCRM.Infrastructure.Commissions;

public class CommissionService : ICommissionService
{
    private static readonly HashSet<string> SortableFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "CreatedAt", "CommissionAmount", "Status"
    };

    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantService _currentTenant;

    public CommissionService(ApplicationDbContext db, ICurrentTenantService currentTenant)
    {
        _db = db;
        _currentTenant = currentTenant;
    }

    public async Task<PagedResult<CommissionDto>> ListAsync(CommissionListQuery query, CancellationToken cancellationToken = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > 100 ? 20 : query.PageSize;

        var commissions = _db.Commissions.AsNoTracking().AsQueryable();

        if (query.Status.HasValue)
        {
            commissions = commissions.Where(c => c.Status == query.Status.Value);
        }

        if (query.DealId.HasValue)
        {
            commissions = commissions.Where(c => c.DealId == query.DealId.Value);
        }

        if (query.AgentId.HasValue)
        {
            commissions = commissions.Where(c => c.AgentId == query.AgentId.Value);
        }

        var sortBy = SortableFields.Contains(query.SortBy ?? string.Empty) ? query.SortBy! : "CreatedAt";
        var descending = !string.Equals(query.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);

        commissions = (sortBy, descending) switch
        {
            ("CommissionAmount", true) => commissions.OrderByDescending(c => c.CommissionAmount),
            ("CommissionAmount", false) => commissions.OrderBy(c => c.CommissionAmount),
            ("Status", true) => commissions.OrderByDescending(c => c.Status),
            ("Status", false) => commissions.OrderBy(c => c.Status),
            (_, true) => commissions.OrderByDescending(c => c.CreatedAt),
            _ => commissions.OrderBy(c => c.CreatedAt)
        };

        var totalCount = await commissions.CountAsync(cancellationToken);
        var items = await commissions.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new PagedResult<CommissionDto>
        {
            Items = items.Select(ToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<CommissionDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var commission = await _db.Commissions.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new AppException("Commission not found.", 404);

        return ToDto(commission);
    }

    public async Task<CommissionDto> CreateAsync(CreateCommissionRequest request, CancellationToken cancellationToken = default)
    {
        EnsureElevatedAccess();

        var deal = await _db.Deals.FirstOrDefaultAsync(d => d.Id == request.DealId, cancellationToken)
            ?? throw new AppException("Deal not found.", 400);

        if (deal.Status != DealStatus.Contracted)
        {
            throw new AppException("Commission can only be created for a Contracted deal.", 400);
        }

        var alreadyExists = await _db.Commissions.AnyAsync(c => c.DealId == deal.Id, cancellationToken);
        if (alreadyExists)
        {
            throw new AppException("A commission already exists for this deal.", 409);
        }

        var commission = new Commission
        {
            Id = Guid.NewGuid(),
            DealId = deal.Id,
            AgentId = deal.SalesAgentId,
            CommissionPercentage = request.CommissionPercentage,
            CommissionAmount = Math.Round(deal.DealValue * request.CommissionPercentage / 100m, 2),
            CompanyCommission = Math.Round(deal.DealValue * request.CompanyCommissionPercentage / 100m, 2),
            Status = CommissionStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Commissions.Add(commission);
        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(commission);
    }

    public async Task<CommissionDto> MarkPaidAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EnsureElevatedAccess();

        var commission = await _db.Commissions.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new AppException("Commission not found.", 404);

        if (commission.Status != CommissionStatus.Pending)
        {
            throw new AppException("Only a Pending commission can be marked paid.", 400);
        }

        commission.Status = CommissionStatus.Paid;
        commission.PaymentDate = DateTime.UtcNow;
        commission.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(commission);
    }

    public async Task<CommissionDto> CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EnsureElevatedAccess();

        var commission = await _db.Commissions.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new AppException("Commission not found.", 404);

        if (commission.Status != CommissionStatus.Pending)
        {
            throw new AppException("Only a Pending commission can be cancelled.", 400);
        }

        commission.Status = CommissionStatus.Cancelled;
        commission.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(commission);
    }

    private void EnsureElevatedAccess()
    {
        var allowed = _currentTenant.IsSuperAdmin ||
            _currentTenant.IsInRole(Roles.CompanyAdmin) ||
            _currentTenant.IsInRole(Roles.SalesManager);

        if (!allowed)
        {
            throw new AppException("You are not authorized to manage commissions.", 403);
        }
    }

    private static CommissionDto ToDto(Commission commission) => new()
    {
        Id = commission.Id,
        DealId = commission.DealId,
        AgentId = commission.AgentId,
        CommissionPercentage = commission.CommissionPercentage,
        CommissionAmount = commission.CommissionAmount,
        CompanyCommission = commission.CompanyCommission,
        Status = commission.Status,
        PaymentDate = commission.PaymentDate,
        CreatedAt = commission.CreatedAt,
        UpdatedAt = commission.UpdatedAt
    };
}
