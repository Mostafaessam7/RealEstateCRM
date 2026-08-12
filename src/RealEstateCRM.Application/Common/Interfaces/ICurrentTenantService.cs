namespace RealEstateCRM.Application.Common.Interfaces;

/// <summary>
/// Resolves tenant identity from the trusted authenticated context (JWT claims).
/// Never populate this from request bodies/query strings.
/// </summary>
public interface ICurrentTenantService
{
    Guid? CompanyId { get; }
    Guid? UserId { get; }
    bool IsSuperAdmin { get; }

    bool IsInRole(string role);
}
