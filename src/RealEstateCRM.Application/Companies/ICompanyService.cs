using RealEstateCRM.Application.Companies.DTOs;

namespace RealEstateCRM.Application.Companies;

public interface ICompanyService
{
    /// <summary>The authenticated user's own company. Cached — see TenantCacheKeys.Settings.</summary>
    Task<CompanyDto> GetCurrentAsync(CancellationToken cancellationToken = default);
}
