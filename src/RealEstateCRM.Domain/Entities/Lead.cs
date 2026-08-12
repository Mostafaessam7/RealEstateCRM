using RealEstateCRM.Domain.Common;
using RealEstateCRM.Domain.Enums;

namespace RealEstateCRM.Domain.Entities;

public class Lead : TenantEntity, ISoftDelete
{
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public LeadSource Source { get; set; }
    public LeadStatus Status { get; set; } = LeadStatus.New;
    public decimal? BudgetMin { get; set; }
    public decimal? BudgetMax { get; set; }
    public string? PreferredLocation { get; set; }
    public string? PropertyType { get; set; }
    public Guid? AssignedAgentId { get; set; }
    public string? Notes { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
}
