using RealEstateCRM.Application.ApiKeys.DTOs;

namespace RealEstateCRM.Application.ApiKeys;

public interface IApiKeyService
{
    Task<IReadOnlyList<ApiKeyDto>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Generates a new key, stores only its hash, and returns the plaintext once.</summary>
    Task<CreatedApiKeyDto> CreateAsync(CreateApiKeyRequest request, CancellationToken cancellationToken = default);

    Task RevokeAsync(Guid id, CancellationToken cancellationToken = default);
}
