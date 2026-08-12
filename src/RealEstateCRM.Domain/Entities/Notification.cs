using RealEstateCRM.Domain.Common;

namespace RealEstateCRM.Domain.Entities;

/// <summary>No UpdatedAt per docs/database.md — notifications are immutable except for IsRead.</summary>
public class Notification : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid UserId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
