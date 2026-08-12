using RealEstateCRM.Application.Common.Models;
using RealEstateCRM.Application.Marketplace.DTOs;

namespace RealEstateCRM.Application.Marketplace;

/// <summary>
/// Deliberately cross-tenant and unauthenticated — the public marketplace. Only ever reads
/// Units explicitly opted in via IsPubliclyListed, and only ever returns PublicUnitDto (never
/// a tenant-owned entity or an internal DTO with CompanyId/financial-terms fields).
/// </summary>
public interface IMarketplaceService
{
    Task<PagedResult<PublicUnitDto>> ListAsync(PublicUnitListQuery query, CancellationToken cancellationToken = default);
}
