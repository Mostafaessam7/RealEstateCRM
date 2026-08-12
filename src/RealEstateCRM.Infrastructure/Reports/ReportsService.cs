using Microsoft.EntityFrameworkCore;
using RealEstateCRM.Application.Reports;
using RealEstateCRM.Application.Reports.DTOs;
using RealEstateCRM.Domain.Enums;
using RealEstateCRM.Infrastructure.Persistence;

namespace RealEstateCRM.Infrastructure.Reports;

public class ReportsService : IReportsService
{
    private readonly ApplicationDbContext _db;

    public ReportsService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<LeadsReportDto> GetLeadsReportAsync(CancellationToken cancellationToken = default)
    {
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

        var byStatus = await _db.Leads
            .GroupBy(l => l.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var bySource = await _db.Leads
            .GroupBy(l => l.Source)
            .Select(g => new { Source = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return new LeadsReportDto
        {
            TotalLeads = byStatus.Sum(s => s.Count),
            NewLeadsLast30Days = await _db.Leads.CountAsync(l => l.CreatedAt >= thirtyDaysAgo, cancellationToken),
            ByStatus = byStatus.ToDictionary(s => s.Status.ToString(), s => s.Count),
            BySource = bySource.ToDictionary(s => s.Source.ToString(), s => s.Count)
        };
    }

    public async Task<SalesReportDto> GetSalesReportAsync(CancellationToken cancellationToken = default)
    {
        var byStatus = await _db.Deals
            .GroupBy(d => d.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var totalSalesValue = await _db.Deals
            .Where(d => d.Status == DealStatus.Contracted)
            .SumAsync(d => (decimal?)d.DealValue, cancellationToken) ?? 0m;

        return new SalesReportDto
        {
            TotalDeals = byStatus.Sum(s => s.Count),
            ContractedDeals = byStatus.FirstOrDefault(s => s.Status == DealStatus.Contracted)?.Count ?? 0,
            TotalSalesValue = totalSalesValue,
            ByStatus = byStatus.ToDictionary(s => s.Status.ToString(), s => s.Count)
        };
    }

    public async Task<ConversionReportDto> GetConversionReportAsync(CancellationToken cancellationToken = default)
    {
        var totalLeads = await _db.Leads.CountAsync(cancellationToken);
        var convertedLeads = await _db.Leads.CountAsync(l => l.Status == LeadStatus.Contracted, cancellationToken);

        return new ConversionReportDto
        {
            TotalLeads = totalLeads,
            ConvertedLeads = convertedLeads,
            ConversionRatePercent = totalLeads == 0 ? 0 : Math.Round(convertedLeads * 100.0 / totalLeads, 1)
        };
    }

    public async Task<IReadOnlyList<AgentPerformanceDto>> GetAgentPerformanceReportAsync(CancellationToken cancellationToken = default)
    {
        var leadsByAgent = await _db.Leads
            .Where(l => l.AssignedAgentId != null)
            .GroupBy(l => l.AssignedAgentId!.Value)
            .Select(g => new { AgentId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var dealsByAgent = await _db.Deals
            .Where(d => d.Status == DealStatus.Contracted)
            .GroupBy(d => d.SalesAgentId)
            .Select(g => new { AgentId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var commissionByAgent = await _db.Commissions
            .Where(c => c.Status != CommissionStatus.Cancelled)
            .GroupBy(c => c.AgentId)
            .Select(g => new { AgentId = g.Key, Total = g.Sum(c => c.CommissionAmount) })
            .ToListAsync(cancellationToken);

        var agentIds = leadsByAgent.Select(l => l.AgentId)
            .Union(dealsByAgent.Select(d => d.AgentId))
            .Union(commissionByAgent.Select(c => c.AgentId))
            .Distinct()
            .ToList();

        var agentNames = await _db.Users
            .Where(u => agentIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);

        return agentIds.Select(agentId => new AgentPerformanceDto
        {
            AgentId = agentId,
            AgentName = agentNames.GetValueOrDefault(agentId, "Unknown"),
            LeadsAssigned = leadsByAgent.FirstOrDefault(l => l.AgentId == agentId)?.Count ?? 0,
            DealsContracted = dealsByAgent.FirstOrDefault(d => d.AgentId == agentId)?.Count ?? 0,
            TotalCommissionEarned = commissionByAgent.FirstOrDefault(c => c.AgentId == agentId)?.Total ?? 0m
        })
        .OrderByDescending(a => a.DealsContracted)
        .ToList();
    }

    public async Task<CommissionReportDto> GetCommissionReportAsync(CancellationToken cancellationToken = default)
    {
        var byStatus = await _db.Commissions
            .GroupBy(c => c.Status)
            .Select(g => new { Status = g.Key, Total = g.Sum(c => c.CommissionAmount) })
            .ToListAsync(cancellationToken);

        return new CommissionReportDto
        {
            TotalPending = byStatus.FirstOrDefault(s => s.Status == CommissionStatus.Pending)?.Total ?? 0m,
            TotalPaid = byStatus.FirstOrDefault(s => s.Status == CommissionStatus.Paid)?.Total ?? 0m,
            TotalCancelled = byStatus.FirstOrDefault(s => s.Status == CommissionStatus.Cancelled)?.Total ?? 0m
        };
    }

    public async Task<InventoryReportDto> GetInventoryReportAsync(CancellationToken cancellationToken = default)
    {
        var byStatus = await _db.Units
            .GroupBy(u => u.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return new InventoryReportDto
        {
            TotalProjects = await _db.Projects.CountAsync(cancellationToken),
            TotalUnits = byStatus.Sum(s => s.Count),
            UnitsByStatus = byStatus.ToDictionary(s => s.Status.ToString(), s => s.Count)
        };
    }
}
