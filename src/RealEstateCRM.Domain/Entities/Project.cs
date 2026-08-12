using RealEstateCRM.Domain.Common;
using RealEstateCRM.Domain.Enums;

namespace RealEstateCRM.Domain.Entities;

public class Project : TenantEntity, ISoftDelete
{
    public string Name { get; set; } = string.Empty;
    public string? Developer { get; set; }
    public string? Location { get; set; }
    public string? Description { get; set; }
    public decimal? StartingPrice { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.Planning;

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
}
