using RealEstateCRM.Application.Reports.DTOs;

namespace RealEstateCRM.Application.Reports;

public interface IReportsService
{
    /// <summary>Also covers the "lead source report" checklist item via BySource.</summary>
    Task<LeadsReportDto> GetLeadsReportAsync(CancellationToken cancellationToken = default);

    Task<SalesReportDto> GetSalesReportAsync(CancellationToken cancellationToken = default);

    Task<ConversionReportDto> GetConversionReportAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AgentPerformanceDto>> GetAgentPerformanceReportAsync(CancellationToken cancellationToken = default);

    Task<CommissionReportDto> GetCommissionReportAsync(CancellationToken cancellationToken = default);

    Task<InventoryReportDto> GetInventoryReportAsync(CancellationToken cancellationToken = default);
}
