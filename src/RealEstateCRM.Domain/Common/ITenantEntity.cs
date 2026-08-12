namespace RealEstateCRM.Domain.Common;

/// <summary>
/// Marks an entity as tenant-owned so Infrastructure can apply EF Core global
/// query filters and tenant-safe writes generically, without per-entity wiring.
/// </summary>
public interface ITenantEntity
{
    Guid CompanyId { get; set; }
}
