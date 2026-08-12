using RealEstateCRM.Domain.Common;

namespace RealEstateCRM.Domain.Entities;

/// <summary>
/// A revocable credential for programmatic/mobile-backend access to the Public API (/api/v1).
/// Only the SHA-256 hash is stored — the plaintext key is shown once, at creation, like a
/// GitHub PAT. KeyPrefix is a short, non-secret identifier shown in the management UI.
/// </summary>
public class ApiKey : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string KeyPrefix { get; set; } = string.Empty;
    public string HashedKey { get; set; } = string.Empty;

    /// <summary>Comma-separated: "read" or "read,write".</summary>
    public string Scopes { get; set; } = "read";

    public bool IsActive { get; set; } = true;
    public Guid CreatedByUserId { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
