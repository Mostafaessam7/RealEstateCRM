using Microsoft.EntityFrameworkCore;
using RealEstateCRM.Application.Common.Caching;
using RealEstateCRM.Application.Common.Exceptions;
using RealEstateCRM.Application.Common.Interfaces;
using RealEstateCRM.Application.Companies;
using RealEstateCRM.Application.Companies.DTOs;
using RealEstateCRM.Infrastructure.Persistence;

namespace RealEstateCRM.Infrastructure.Companies;

public class CompanyService : ICompanyService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantService _currentTenant;
    private readonly ICacheService _cache;

    public CompanyService(ApplicationDbContext db, ICurrentTenantService currentTenant, ICacheService cache)
    {
        _db = db;
        _currentTenant = currentTenant;
        _cache = cache;
    }

    public async Task<CompanyDto> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        var companyId = _currentTenant.CompanyId
            ?? throw new AppException("Authenticated company context is required.", 401);

        return await _cache.GetOrCreateAsync(
            TenantCacheKeys.Settings(companyId),
            async ct =>
            {
                var company = await _db.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == companyId, ct)
                    ?? throw new AppException("Company not found.", 404);

                return new CompanyDto
                {
                    Id = company.Id,
                    Name = company.Name,
                    Slug = company.Slug,
                    Phone = company.Phone,
                    Email = company.Email,
                    IsActive = company.IsActive
                };
            },
            CacheTtl,
            cancellationToken);
    }
}
