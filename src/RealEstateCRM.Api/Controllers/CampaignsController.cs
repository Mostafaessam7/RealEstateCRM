using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateCRM.Application.Common.Validation;
using RealEstateCRM.Application.Marketing;
using RealEstateCRM.Application.Marketing.DTOs;

namespace RealEstateCRM.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/campaigns")]
public class CampaignsController : ControllerBase
{
    private readonly ICampaignService _campaignService;
    private readonly IValidator<CreateCampaignRequest> _createValidator;

    public CampaignsController(ICampaignService campaignService, IValidator<CreateCampaignRequest> createValidator)
    {
        _campaignService = campaignService;
        _createValidator = createValidator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CampaignDto>>> List(CancellationToken cancellationToken)
    {
        return Ok(await _campaignService.ListAsync(cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CampaignDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _campaignService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<CampaignDto>> Create(CreateCampaignRequest request, CancellationToken cancellationToken)
    {
        await _createValidator.ValidateAndThrowAppExceptionAsync(request, cancellationToken);
        var campaign = await _campaignService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = campaign.Id }, campaign);
    }

    [HttpPost("{id:guid}/send")]
    public async Task<ActionResult<CampaignDto>> Send(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _campaignService.SendAsync(id, cancellationToken));
    }

    [HttpGet("{id:guid}/recipients")]
    public async Task<ActionResult<IReadOnlyList<CampaignRecipientDto>>> ListRecipients(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _campaignService.ListRecipientsAsync(id, cancellationToken));
    }
}
