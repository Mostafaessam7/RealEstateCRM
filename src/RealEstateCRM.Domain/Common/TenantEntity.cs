namespace RealEstateCRM.Domain.Common;

public abstract class TenantEntity : BaseEntity, ITenantEntity
{
    public Guid CompanyId { get; set; }
}
