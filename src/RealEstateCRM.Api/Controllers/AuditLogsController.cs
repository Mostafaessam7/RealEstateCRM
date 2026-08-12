using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateCRM.Application.Auditing;
using RealEstateCRM.Application.Auditing.DTOs;
using RealEstateCRM.Application.Common.Models;
using RealEstateCRM.Domain.Constants;

namespace RealEstateCRM.Api.Controllers;

[ApiController]
[Authorize(Roles = $"{Roles.CompanyAdmin},{Roles.SuperAdmin}")]
[Route("api/audit-logs")]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogsController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> List([FromQuery] AuditLogListQuery query, CancellationToken cancellationToken)
    {
        return Ok(await _auditLogService.ListAsync(query, cancellationToken));
    }
}
