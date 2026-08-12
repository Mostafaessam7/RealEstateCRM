using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RealEstateCRM.Application.Common.Models;
using RealEstateCRM.Application.Marketplace;
using RealEstateCRM.Application.Marketplace.DTOs;

namespace RealEstateCRM.Api.Controllers;

/// <summary>
/// The only unauthenticated, cross-tenant surface in the app — deliberately so. Only ever
/// exposes Units a company explicitly opted in via IsPubliclyListed, through PublicUnitDto
/// (no CompanyId, no internal ids, no financial terms beyond price). See IMarketplaceService.
/// </summary>
[ApiController]
[EnableRateLimiting("Marketplace")]
[Route("api/marketplace")]
public class MarketplaceController : ControllerBase
{
    private readonly IMarketplaceService _marketplaceService;

    public MarketplaceController(IMarketplaceService marketplaceService)
    {
        _marketplaceService = marketplaceService;
    }

    [HttpGet("units")]
    public async Task<ActionResult<PagedResult<PublicUnitDto>>> ListUnits([FromQuery] PublicUnitListQuery query, CancellationToken cancellationToken)
    {
        return Ok(await _marketplaceService.ListAsync(query, cancellationToken));
    }
}
