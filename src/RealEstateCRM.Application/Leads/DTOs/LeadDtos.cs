using RealEstateCRM.Domain.Enums;

namespace RealEstateCRM.Application.Leads.DTOs;

public class LeadDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public LeadSource Source { get; set; }
    public LeadStatus Status { get; set; }
    public decimal? BudgetMin { get; set; }
    public decimal? BudgetMax { get; set; }
    public string? PreferredLocation { get; set; }
    public string? PropertyType { get; set; }
    public Guid? AssignedAgentId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>Never contains CompanyId — that comes from the authenticated context.</summary>
public class CreateLeadRequest
{
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public LeadSource Source { get; set; }
    public decimal? BudgetMin { get; set; }
    public decimal? BudgetMax { get; set; }
    public string? PreferredLocation { get; set; }
    public string? PropertyType { get; set; }
    public Guid? AssignedAgentId { get; set; }
    public string? Notes { get; set; }
}

public class UpdateLeadRequest : CreateLeadRequest
{
    public LeadStatus Status { get; set; }
}

public class LeadListQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public LeadStatus? Status { get; set; }
    public Guid? AssignedAgentId { get; set; }
    public LeadSource? Source { get; set; }
    public string? SortBy { get; set; }
    public string SortDirection { get; set; } = "desc";
}

public class AssignLeadRequest
{
    public Guid AgentId { get; set; }
}

public class LeadActivityDto
{
    public Guid Id { get; set; }
    public Guid LeadId { get; set; }
    public Guid UserId { get; set; }
    public LeadActivityType Type { get; set; }
    public string? Description { get; set; }
    public DateTime ActivityDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateLeadActivityRequest
{
    public LeadActivityType Type { get; set; }
    public string? Description { get; set; }

    /// <summary>Defaults to now when omitted. For Type == FollowUp, set this to the scheduled follow-up time.</summary>
    public DateTime? ActivityDate { get; set; }
}
