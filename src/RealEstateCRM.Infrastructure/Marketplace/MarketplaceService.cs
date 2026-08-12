using Microsoft.EntityFrameworkCore;
using RealEstateCRM.Application.Common.Models;
using RealEstateCRM.Application.Marketplace;
using RealEstateCRM.Application.Marketplace.DTOs;
using RealEstateCRM.Domain.Entities;
using RealEstateCRM.Domain.Enums;
using RealEstateCRM.Infrastructure.Persistence;

namespace RealEstateCRM.Infrastructure.Marketplace;

public class MarketplaceService : IMarketplaceService
{
    private readonly ApplicationDbContext _db;

    public MarketplaceService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<PublicUnitDto>> ListAsync(PublicUnitListQuery query, CancellationToken cancellationToken = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > 50 ? 20 : query.PageSize;

        var units = _db.ForAllTenants<Unit>()
            .Where(u => u.IsPubliclyListed && u.Status == UnitStatus.Available && !u.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.PropertyType))
        {
            units = units.Where(u => u.PropertyType == query.PropertyType);
        }

        if (!string.IsNullOrWhiteSpace(query.Location))
        {
            units = units.Where(u => u.Location != null && u.Location.Contains(query.Location));
        }

        if (query.MinPrice.HasValue)
        {
            units = units.Where(u => u.Price >= query.MinPrice.Value);
        }

        if (query.MaxPrice.HasValue)
        {
            units = units.Where(u => u.Price <= query.MaxPrice.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            units = units.Where(u => u.UnitCode.Contains(search) || (u.Location != null && u.Location.Contains(search)));
        }

        var projected = from u in units
                         join p in _db.ForAllTenants<Project>() on u.ProjectId equals p.Id
                         join c in _db.Companies on u.CompanyId equals c.Id
                         orderby u.CreatedAt descending
                         select new PublicUnitDto
                         {
                             UnitId = u.Id,
                             UnitCode = u.UnitCode,
                             PropertyType = u.PropertyType,
                             Price = u.Price,
                             Area = u.Area,
                             Bedrooms = u.Bedrooms,
                             Bathrooms = u.Bathrooms,
                             Location = u.Location,
                             Description = u.Description,
                             ProjectName = p.Name,
                             CompanyName = c.Name
                         };

        var totalCount = await projected.CountAsync(cancellationToken);
        var items = await projected.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new PagedResult<PublicUnitDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}
