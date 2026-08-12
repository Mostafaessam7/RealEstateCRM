using RealEstateCRM.Domain.Enums;

namespace RealEstateCRM.Application.Commissions.DTOs;

public class CommissionDto
{
    public Guid Id { get; set; }
    public Guid DealId { get; set; }
    public Guid AgentId { get; set; }
    public decimal CommissionPercentage { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal CompanyCommission { get; set; }
    public CommissionStatus Status { get; set; }
    public DateTime? PaymentDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// AgentId is not settable — it's taken from the deal's SalesAgentId. Only valid for a
/// Contracted deal. CommissionAmount/CompanyCommission are computed from DealValue.
/// </summary>
public class CreateCommissionRequest
{
    public Guid DealId { get; set; }

    /// <summary>Agent's cut, as a percentage of DealValue.</summary>
    public decimal CommissionPercentage { get; set; }

    /// <summary>Company's cut, as a percentage of DealValue.</summary>
    public decimal CompanyCommissionPercentage { get; set; }
}

public class CommissionListQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public CommissionStatus? Status { get; set; }
    public Guid? DealId { get; set; }
    public Guid? AgentId { get; set; }
    public string? SortBy { get; set; }
    public string SortDirection { get; set; } = "desc";
}
