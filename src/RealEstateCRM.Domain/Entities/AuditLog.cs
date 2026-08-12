using RealEstateCRM.Domain.Common;

namespace RealEstateCRM.Domain.Entities;

/// <summary>Immutable — no UpdatedAt, never edited or deleted.</summary>
public class AuditLog : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }

    /// <summary>Null for system-initiated changes (e.g. a background job) rather than a user action.</summary>
    public Guid? UserId { get; set; }

    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}
