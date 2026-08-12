using RealEstateCRM.Application.Auditing.DTOs;
using RealEstateCRM.Application.Common.Models;

namespace RealEstateCRM.Application.Auditing;

/// <summary>CompanyAdmin/SuperAdmin only — see docs/roadmap.md Phase 17.</summary>
public interface IAuditLogService
{
    Task<PagedResult<AuditLogDto>> ListAsync(AuditLogListQuery query, CancellationToken cancellationToken = default);
}
