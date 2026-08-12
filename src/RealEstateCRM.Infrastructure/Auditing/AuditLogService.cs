using Microsoft.EntityFrameworkCore;
using RealEstateCRM.Application.Auditing;
using RealEstateCRM.Application.Auditing.DTOs;
using RealEstateCRM.Application.Common.Models;
using RealEstateCRM.Infrastructure.Persistence;

namespace RealEstateCRM.Infrastructure.Auditing;

public class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _db;

    public AuditLogService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<AuditLogDto>> ListAsync(AuditLogListQuery query, CancellationToken cancellationToken = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > 100 ? 20 : query.PageSize;

        var logs = _db.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.EntityName))
        {
            logs = logs.Where(l => l.EntityName == query.EntityName);
        }

        if (query.EntityId.HasValue)
        {
            logs = logs.Where(l => l.EntityId == query.EntityId.Value);
        }

        logs = logs.OrderByDescending(l => l.CreatedAt);

        var totalCount = await logs.CountAsync(cancellationToken);
        var items = await logs.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new PagedResult<AuditLogDto>
        {
            Items = items.Select(l => new AuditLogDto
            {
                Id = l.Id,
                UserId = l.UserId,
                Action = l.Action,
                EntityName = l.EntityName,
                EntityId = l.EntityId,
                OldValues = l.OldValues,
                NewValues = l.NewValues,
                IpAddress = l.IpAddress,
                CreatedAt = l.CreatedAt
            }).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}
