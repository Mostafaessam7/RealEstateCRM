using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateCRM.Application.Reports;
using RealEstateCRM.Application.Reports.DTOs;

namespace RealEstateCRM.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly IReportsService _reportsService;

    public ReportsController(IReportsService reportsService)
    {
        _reportsService = reportsService;
    }

    [HttpGet("leads")]
    public async Task<ActionResult<LeadsReportDto>> GetLeadsReport(CancellationToken cancellationToken)
    {
        return Ok(await _reportsService.GetLeadsReportAsync(cancellationToken));
    }

    [HttpGet("sales")]
    public async Task<ActionResult<SalesReportDto>> GetSalesReport(CancellationToken cancellationToken)
    {
        return Ok(await _reportsService.GetSalesReportAsync(cancellationToken));
    }

    [HttpGet("conversion")]
    public async Task<ActionResult<ConversionReportDto>> GetConversionReport(CancellationToken cancellationToken)
    {
        return Ok(await _reportsService.GetConversionReportAsync(cancellationToken));
    }

    [HttpGet("agent-performance")]
    public async Task<ActionResult<IReadOnlyList<AgentPerformanceDto>>> GetAgentPerformanceReport(CancellationToken cancellationToken)
    {
        return Ok(await _reportsService.GetAgentPerformanceReportAsync(cancellationToken));
    }

    [HttpGet("commissions")]
    public async Task<ActionResult<CommissionReportDto>> GetCommissionReport(CancellationToken cancellationToken)
    {
        return Ok(await _reportsService.GetCommissionReportAsync(cancellationToken));
    }

    [HttpGet("inventory")]
    public async Task<ActionResult<InventoryReportDto>> GetInventoryReport(CancellationToken cancellationToken)
    {
        return Ok(await _reportsService.GetInventoryReportAsync(cancellationToken));
    }
}
