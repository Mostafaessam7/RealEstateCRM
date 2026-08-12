using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using RealEstateCRM.Application.Common.Models;
using RealEstateCRM.Application.Common.Validation;
using RealEstateCRM.Application.Leads;
using RealEstateCRM.Application.Leads.DTOs;

namespace RealEstateCRM.Api.Controllers.V1;

[Route("api/v1/leads")]
public class PublicLeadsController : PublicApiControllerBase
{
    private readonly ILeadService _leadService;
    private readonly IValidator<CreateLeadRequest> _createValidator;
    private readonly IValidator<UpdateLeadRequest> _updateValidator;

    public PublicLeadsController(ILeadService leadService, IValidator<CreateLeadRequest> createValidator, IValidator<UpdateLeadRequest> updateValidator)
    {
        _leadService = leadService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<LeadDto>>> List([FromQuery] LeadListQuery query, CancellationToken cancellationToken)
    {
        return Ok(await _leadService.ListAsync(query, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LeadDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _leadService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<LeadDto>> Create(CreateLeadRequest request, CancellationToken cancellationToken)
    {
        EnsureWriteScope();
        await _createValidator.ValidateAndThrowAppExceptionAsync(request, cancellationToken);
        var lead = await _leadService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = lead.Id }, lead);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<LeadDto>> Update(Guid id, UpdateLeadRequest request, CancellationToken cancellationToken)
    {
        EnsureWriteScope();
        await _updateValidator.ValidateAndThrowAppExceptionAsync(request, cancellationToken);
        return Ok(await _leadService.UpdateAsync(id, request, cancellationToken));
    }
}
