using Microsoft.AspNetCore.Mvc;
using RealEstateCRM.Application.Common.Models;
using RealEstateCRM.Application.Units;
using RealEstateCRM.Application.Units.DTOs;

namespace RealEstateCRM.Api.Controllers.V1;

[Route("api/v1/units")]
public class PublicUnitsController : PublicApiControllerBase
{
    private readonly IUnitService _unitService;

    public PublicUnitsController(IUnitService unitService)
    {
        _unitService = unitService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<UnitDto>>> List([FromQuery] UnitListQuery query, CancellationToken cancellationToken)
    {
        return Ok(await _unitService.ListAsync(query, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UnitDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _unitService.GetByIdAsync(id, cancellationToken));
    }

    [HttpGet("available")]
    public async Task<ActionResult<IReadOnlyList<UnitDto>>> GetAvailable([FromQuery] Guid? projectId, CancellationToken cancellationToken)
    {
        return Ok(await _unitService.GetAvailableAsync(projectId, cancellationToken));
    }
}
