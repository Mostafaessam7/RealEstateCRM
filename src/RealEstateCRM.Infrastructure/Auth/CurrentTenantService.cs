using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using RealEstateCRM.Application.Common.Interfaces;
using RealEstateCRM.Domain.Constants;

namespace RealEstateCRM.Infrastructure.Auth;

/// <summary>
/// Resolves tenant identity from the authenticated user's JWT claims via HttpContext.
/// Returns nulls/false when there is no authenticated HTTP context (e.g. design-time
/// tooling or background jobs) — callers must not treat that as SuperAdmin/cross-tenant access.
/// </summary>
public class CurrentTenantService : ICurrentTenantService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentTenantService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var value = Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public Guid? CompanyId
    {
        get
        {
            var value = Principal?.FindFirstValue("company_id");
            if (Guid.TryParse(value, out var id))
            {
                return id;
            }

            // No HTTP context (background job) — use the tenant explicitly declared via ITenantScope.
            return AmbientTenantContext.CompanyId;
        }
    }

    public bool IsSuperAdmin => Principal?.IsInRole(Roles.SuperAdmin) ?? false;

    public bool IsInRole(string role) => Principal?.IsInRole(role) ?? false;
}
