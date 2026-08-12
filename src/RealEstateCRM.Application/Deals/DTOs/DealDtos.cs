using RealEstateCRM.Domain.Enums;

namespace RealEstateCRM.Application.Deals.DTOs;

public class DealDto
{
    public Guid Id { get; set; }
    public Guid LeadId { get; set; }
    public Guid UnitId { get; set; }
    public Guid SalesAgentId { get; set; }
    public decimal DealValue { get; set; }
    public DealStatus Status { get; set; }
    public DateTime? ReservationDate { get; set; }
    public DateTime? ContractDate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Never contains CompanyId. SalesAgentId is optional — a SalesAgent caller is always
/// forced to themself; only CompanyAdmin/SalesManager/SuperAdmin may assign someone else.
/// </summary>
public class CreateDealRequest
{
    public Guid LeadId { get; set; }
    public Guid UnitId { get; set; }
    public Guid? SalesAgentId { get; set; }
    public decimal DealValue { get; set; }
    public string? Notes { get; set; }
}

/// <summary>Field edits only — status transitions go through Reserve/Contract/Cancel.</summary>
public class UpdateDealRequest
{
    public decimal DealValue { get; set; }
    public string? Notes { get; set; }
}

public class DealListQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public DealStatus? Status { get; set; }
    public Guid? LeadId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? SalesAgentId { get; set; }
    public string? SortBy { get; set; }
    public string SortDirection { get; set; } = "desc";
}
