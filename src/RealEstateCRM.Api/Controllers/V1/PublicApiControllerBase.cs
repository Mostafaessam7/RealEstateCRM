using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RealEstateCRM.Application.Common.Exceptions;
using RealEstateCRM.Infrastructure.Auth;

namespace RealEstateCRM.Api.Controllers.V1;

/// <summary>
/// Base for the Public API (/api/v1). Accepts either a normal user JWT (Bearer — full access,
/// permissions as today) or a company API key (X-Api-Key — scoped to "read" or "read,write").
/// Rate limited via the "PublicApi" policy registered in Program.cs. See docs/public-api.md.
/// </summary>
[ApiController]
[Authorize(AuthenticationSchemes = "Bearer,ApiKey")]
[EnableRateLimiting("PublicApi")]
public abstract class PublicApiControllerBase : ControllerBase
{
    /// <summary>Throws 403 if the request was authenticated by an API key without "write" scope.</summary>
    protected void EnsureWriteScope()
    {
        if (User.Identity?.AuthenticationType != ApiKeyAuthenticationSchemeOptions.SchemeName)
        {
            return;
        }

        var scopes = User.FindFirst(ApiKeyAuthenticationHandler.ScopeClaimType)?.Value ?? string.Empty;
        if (!scopes.Split(',').Contains("write"))
        {
            throw new AppException("This API key has read-only access.", 403);
        }
    }
}
