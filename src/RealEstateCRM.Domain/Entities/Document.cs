using RealEstateCRM.Domain.Common;

namespace RealEstateCRM.Domain.Entities;

/// <summary>
/// A generic uploaded file, optionally linked to a Lead or a Deal (both nullable — a
/// document doesn't have to be tied to either). Immutable — no UpdatedAt.
/// </summary>
public class Document : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string BlobPath { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public Guid UploadedByUserId { get; set; }
    public Guid? LeadId { get; set; }
    public Guid? DealId { get; set; }
    public DateTime CreatedAt { get; set; }
}
