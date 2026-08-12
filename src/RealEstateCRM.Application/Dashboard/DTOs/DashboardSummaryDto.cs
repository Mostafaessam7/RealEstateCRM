namespace RealEstateCRM.Application.Dashboard.DTOs;

/// <summary>KPIs per docs/frontend.md#dashboard.</summary>
public class DashboardSummaryDto
{
    public int TotalLeads { get; set; }
    public int NewLeadsLast30Days { get; set; }
    public double ConversionRatePercent { get; set; }
    public int TotalDeals { get; set; }
    public int TotalActiveDeals { get; set; }
    public decimal TotalSalesValue { get; set; }
    public int UpcomingFollowUps { get; set; }
    public int TotalAvailableUnits { get; set; }
}
