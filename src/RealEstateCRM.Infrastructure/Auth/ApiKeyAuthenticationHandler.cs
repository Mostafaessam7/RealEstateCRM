using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RealEstateCRM.Domain.Constants;
using RealEstateCRM.Infrastructure.ApiKeys;
using RealEstateCRM.Infrastructure.Persistence;

namespace RealEstateCRM.Infrastructure.Auth;

public class ApiKeyAuthenticationSchemeOptions : AuthenticationSchemeOptions
{
    public const string SchemeName = "ApiKey";
    public const string HeaderName = "X-Api-Key";
}

/// <summary>
/// Authenticates Public API (/api/v1) requests carrying an X-Api-Key header. On success,
/// builds a ClaimsPrincipal carrying the key's CompanyId (never trusted from the request
/// itself) and a scope claim used by PublicApiControllerBase to gate write operations.
/// </summary>
public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationSchemeOptions>
{
    public const string ScopeClaimType = "api_scope";

    private readonly ApplicationDbContext _db;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ApplicationDbContext db)
        : base(options, logger, encoder)
    {
        _db = db;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyAuthenticationSchemeOptions.HeaderName, out var provided) || provided.Count == 0)
        {
            return AuthenticateResult.NoResult();
        }

        var plaintextKey = provided.ToString();
        var hashedKey = ApiKeyHasher.Hash(plaintextKey);

        var apiKey = await _db.ApiKeys.FirstOrDefaultAsync(k => k.HashedKey == hashedKey);
        if (apiKey is null || !apiKey.IsActive)
        {
            return AuthenticateResult.Fail("Invalid or revoked API key.");
        }

        if (apiKey.ExpiresAt.HasValue && apiKey.ExpiresAt.Value < DateTime.UtcNow)
        {
            return AuthenticateResult.Fail("API key has expired.");
        }

        apiKey.LastUsedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, apiKey.CreatedByUserId.ToString()),
            new("company_id", apiKey.CompanyId.ToString()),
            new(ClaimTypes.Role, Roles.SalesAgent),
            new(ScopeClaimType, apiKey.Scopes)
        };

        var identity = new ClaimsIdentity(claims, ApiKeyAuthenticationSchemeOptions.SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, ApiKeyAuthenticationSchemeOptions.SchemeName);

        return AuthenticateResult.Success(ticket);
    }
}
