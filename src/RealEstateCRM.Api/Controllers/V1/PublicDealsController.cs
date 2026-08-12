using Microsoft.AspNetCore.Mvc;
using RealEstateCRM.Application.Common.Models;
using RealEstateCRM.Application.Deals;
using RealEstateCRM.Application.Deals.DTOs;

namespace RealEstateCRM.Api.Controllers.V1;

[Route("api/v1/deals")]
public class PublicDealsController : PublicApiControllerBase
{
    private readonly IDealService _dealService;

    public PublicDealsController(IDealService dealService)
    {
        _dealService = dealService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<DealDto>>> List([FromQuery] DealListQuery query, CancellationToken cancellationToken)
    {
        return Ok(await _dealService.ListAsync(query, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DealDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _dealService.GetByIdAsync(id, cancellationToken));
    }
}
