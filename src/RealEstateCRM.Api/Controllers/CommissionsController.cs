using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateCRM.Application.Commissions;
using RealEstateCRM.Application.Commissions.DTOs;
using RealEstateCRM.Application.Common.Models;
using RealEstateCRM.Application.Common.Validation;

namespace RealEstateCRM.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/commissions")]
public class CommissionsController : ControllerBase
{
    private readonly ICommissionService _commissionService;
    private readonly IValidator<CreateCommissionRequest> _createValidator;

    public CommissionsController(ICommissionService commissionService, IValidator<CreateCommissionRequest> createValidator)
    {
        _commissionService = commissionService;
        _createValidator = createValidator;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<CommissionDto>>> List([FromQuery] CommissionListQuery query, CancellationToken cancellationToken)
    {
        return Ok(await _commissionService.ListAsync(query, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CommissionDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _commissionService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<CommissionDto>> Create(CreateCommissionRequest request, CancellationToken cancellationToken)
    {
        await _createValidator.ValidateAndThrowAppExceptionAsync(request, cancellationToken);
        var commission = await _commissionService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = commission.Id }, commission);
    }

    [HttpPost("{id:guid}/mark-paid")]
    public async Task<ActionResult<CommissionDto>> MarkPaid(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _commissionService.MarkPaidAsync(id, cancellationToken));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<CommissionDto>> Cancel(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _commissionService.CancelAsync(id, cancellationToken));
    }
}
