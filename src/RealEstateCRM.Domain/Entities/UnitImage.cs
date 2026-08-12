using RealEstateCRM.Domain.Common;

namespace RealEstateCRM.Domain.Entities;

/// <summary>Immutable — no UpdatedAt. Deleting an image removes the row (not a soft delete).</summary>
public class UnitImage : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid UnitId { get; set; }
    public string BlobPath { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
}
