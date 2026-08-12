using Microsoft.EntityFrameworkCore;
using RealEstateCRM.Application.ApiKeys;
using RealEstateCRM.Application.ApiKeys.DTOs;
using RealEstateCRM.Application.Common.Exceptions;
using RealEstateCRM.Application.Common.Interfaces;
using RealEstateCRM.Domain.Entities;
using RealEstateCRM.Infrastructure.Persistence;

namespace RealEstateCRM.Infrastructure.ApiKeys;

public class ApiKeyService : IApiKeyService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantService _currentTenant;

    public ApiKeyService(ApplicationDbContext db, ICurrentTenantService currentTenant)
    {
        _db = db;
        _currentTenant = currentTenant;
    }

    public async Task<IReadOnlyList<ApiKeyDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var keys = await _db.ApiKeys.AsNoTracking()
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync(cancellationToken);

        return keys.Select(ToDto).ToList();
    }

    public async Task<CreatedApiKeyDto> CreateAsync(CreateApiKeyRequest request, CancellationToken cancellationToken = default)
    {
        var userId = _currentTenant.UserId ?? throw new AppException("Authenticated user context is required.", 401);

        var plaintextKey = ApiKeyHasher.GenerateKey();

        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            KeyPrefix = ApiKeyHasher.DisplayPrefix(plaintextKey),
            HashedKey = ApiKeyHasher.Hash(plaintextKey),
            Scopes = request.Scopes,
            IsActive = true,
            CreatedByUserId = userId,
            ExpiresAt = request.ExpiresAt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.ApiKeys.Add(apiKey);
        await _db.SaveChangesAsync(cancellationToken);

        var dto = ToDto(apiKey);
        return new CreatedApiKeyDto
        {
            Id = dto.Id,
            Name = dto.Name,
            KeyPrefix = dto.KeyPrefix,
            Scopes = dto.Scopes,
            IsActive = dto.IsActive,
            LastUsedAt = dto.LastUsedAt,
            ExpiresAt = dto.ExpiresAt,
            CreatedAt = dto.CreatedAt,
            PlaintextKey = plaintextKey
        };
    }

    public async Task RevokeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var apiKey = await _db.ApiKeys.FirstOrDefaultAsync(k => k.Id == id, cancellationToken)
            ?? throw new AppException("API key not found.", 404);

        apiKey.IsActive = false;
        apiKey.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static ApiKeyDto ToDto(ApiKey key) => new()
    {
        Id = key.Id,
        Name = key.Name,
        KeyPrefix = key.KeyPrefix,
        Scopes = key.Scopes,
        IsActive = key.IsActive,
        LastUsedAt = key.LastUsedAt,
        ExpiresAt = key.ExpiresAt,
        CreatedAt = key.CreatedAt
    };
}
