using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateCRM.Application.Common.Models;
using RealEstateCRM.Application.Common.Validation;
using RealEstateCRM.Application.Deals;
using RealEstateCRM.Application.Deals.DTOs;

namespace RealEstateCRM.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/deals")]
public class DealsController : ControllerBase
{
    private readonly IDealService _dealService;
    private readonly IValidator<CreateDealRequest> _createValidator;
    private readonly IValidator<UpdateDealRequest> _updateValidator;

    public DealsController(
        IDealService dealService,
        IValidator<CreateDealRequest> createValidator,
        IValidator<UpdateDealRequest> updateValidator)
    {
        _dealService = dealService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
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

    [HttpPost]
    public async Task<ActionResult<DealDto>> Create(CreateDealRequest request, CancellationToken cancellationToken)
    {
        await _createValidator.ValidateAndThrowAppExceptionAsync(request, cancellationToken);
        var deal = await _dealService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = deal.Id }, deal);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<DealDto>> Update(Guid id, UpdateDealRequest request, CancellationToken cancellationToken)
    {
        await _updateValidator.ValidateAndThrowAppExceptionAsync(request, cancellationToken);
        return Ok(await _dealService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/reserve")]
    public async Task<ActionResult<DealDto>> Reserve(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _dealService.ReserveAsync(id, cancellationToken));
    }

    [HttpPost("{id:guid}/contract")]
    public async Task<ActionResult<DealDto>> Contract(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _dealService.ContractAsync(id, cancellationToken));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<DealDto>> Cancel(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _dealService.CancelAsync(id, cancellationToken));
    }
}
