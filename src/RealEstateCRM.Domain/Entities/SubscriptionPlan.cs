using RealEstateCRM.Domain.Common;

namespace RealEstateCRM.Domain.Entities;

/// <summary>Global plan catalog, not tenant-owned. Managed by SuperAdmin.</summary>
public class SubscriptionPlan : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }
    public int MaxUsers { get; set; }
    public int MaxLeads { get; set; }
    public int MaxUnits { get; set; }
    public bool IsActive { get; set; } = true;
}
