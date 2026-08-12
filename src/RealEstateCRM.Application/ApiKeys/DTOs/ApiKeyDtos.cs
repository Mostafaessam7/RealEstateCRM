namespace RealEstateCRM.Application.ApiKeys.DTOs;

public class ApiKeyDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string KeyPrefix { get; set; } = string.Empty;
    public string Scopes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Returned only once, at creation — the plaintext key is never retrievable again.</summary>
public class CreatedApiKeyDto : ApiKeyDto
{
    public string PlaintextKey { get; set; } = string.Empty;
}

public class CreateApiKeyRequest
{
    public string Name { get; set; } = string.Empty;

    /// <summary>"read" or "read,write".</summary>
    public string Scopes { get; set; } = "read";

    public DateTime? ExpiresAt { get; set; }
}
