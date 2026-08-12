namespace RealEstateCRM.Application.Reports.DTOs;

public class LeadsReportDto
{
    public int TotalLeads { get; set; }
    public int NewLeadsLast30Days { get; set; }
    public Dictionary<string, int> ByStatus { get; set; } = new();
    public Dictionary<string, int> BySource { get; set; } = new();
}

public class SalesReportDto
{
    public int TotalDeals { get; set; }
    public int ContractedDeals { get; set; }
    public decimal TotalSalesValue { get; set; }
    public Dictionary<string, int> ByStatus { get; set; } = new();
}

public class ConversionReportDto
{
    public int TotalLeads { get; set; }
    public int ConvertedLeads { get; set; }
    public double ConversionRatePercent { get; set; }
}

public class AgentPerformanceDto
{
    public Guid AgentId { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public int LeadsAssigned { get; set; }
    public int DealsContracted { get; set; }
    public decimal TotalCommissionEarned { get; set; }
}

public class CommissionReportDto
{
    public decimal TotalPending { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalCancelled { get; set; }
}

public class InventoryReportDto
{
    public int TotalProjects { get; set; }
    public int TotalUnits { get; set; }
    public Dictionary<string, int> UnitsByStatus { get; set; } = new();
}
