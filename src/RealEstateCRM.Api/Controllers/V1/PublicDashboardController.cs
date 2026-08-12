using Microsoft.AspNetCore.Mvc;
using RealEstateCRM.Application.Dashboard;
using RealEstateCRM.Application.Dashboard.DTOs;

namespace RealEstateCRM.Api.Controllers.V1;

[Route("api/v1/dashboard")]
public class PublicDashboardController : PublicApiControllerBase
{
    private readonly IDashboardService _dashboardService;

    public PublicDashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary(CancellationToken cancellationToken)
    {
        return Ok(await _dashboardService.GetSummaryAsync(cancellationToken));
    }
}
