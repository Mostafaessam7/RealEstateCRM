using RealEstateCRM.Domain.Common;
using RealEstateCRM.Domain.Enums;

namespace RealEstateCRM.Domain.Entities;

public class Unit : TenantEntity, ISoftDelete
{
    public Guid ProjectId { get; set; }
    public string UnitCode { get; set; } = string.Empty;
    public string? PropertyType { get; set; }
    public decimal Price { get; set; }
    public decimal? Area { get; set; }
    public int? Bedrooms { get; set; }
    public int? Bathrooms { get; set; }
    public string? Floor { get; set; }
    public string? Location { get; set; }
    public UnitStatus Status { get; set; } = UnitStatus.Available;

    /// <summary>Opts this unit into the public, unauthenticated cross-tenant marketplace listing.</summary>
    public bool IsPubliclyListed { get; set; }
    public decimal? DownPayment { get; set; }
    public int? InstallmentYears { get; set; }
    public string? Description { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
}
