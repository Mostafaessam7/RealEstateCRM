using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateCRM.Application.Common.Models;
using RealEstateCRM.Application.Common.Validation;
using RealEstateCRM.Application.AiAssistant;
using RealEstateCRM.Application.AiAssistant.DTOs;
using RealEstateCRM.Application.Leads;
using RealEstateCRM.Application.Leads.DTOs;
using RealEstateCRM.Application.Recommendations;
using RealEstateCRM.Application.Recommendations.DTOs;

namespace RealEstateCRM.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/leads")]
public class LeadsController : ControllerBase
{
    private readonly ILeadService _leadService;
    private readonly ILeadActivityService _leadActivityService;
    private readonly IRecommendationService _recommendationService;
    private readonly IAiLeadAssistantService _aiLeadAssistantService;
    private readonly IValidator<CreateLeadRequest> _createValidator;
    private readonly IValidator<UpdateLeadRequest> _updateValidator;
    private readonly IValidator<AssignLeadRequest> _assignValidator;
    private readonly IValidator<CreateLeadActivityRequest> _activityValidator;

    public LeadsController(
        ILeadService leadService,
        ILeadActivityService leadActivityService,
        IRecommendationService recommendationService,
        IAiLeadAssistantService aiLeadAssistantService,
        IValidator<CreateLeadRequest> createValidator,
        IValidator<UpdateLeadRequest> updateValidator,
        IValidator<AssignLeadRequest> assignValidator,
        IValidator<CreateLeadActivityRequest> activityValidator)
    {
        _leadService = leadService;
        _leadActivityService = leadActivityService;
        _recommendationService = recommendationService;
        _aiLeadAssistantService = aiLeadAssistantService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _assignValidator = assignValidator;
        _activityValidator = activityValidator;
    }

    [HttpGet("{id:guid}/recommendations")]
    public async Task<ActionResult<IReadOnlyList<UnitRecommendationDto>>> GetRecommendations(Guid id, [FromQuery] int count, CancellationToken cancellationToken)
    {
        return Ok(await _recommendationService.GetRecommendationsForLeadAsync(id, count == 0 ? 5 : count, cancellationToken));
    }

    [HttpGet("{id:guid}/ai-insight")]
    public async Task<ActionResult<AiLeadInsightDto>> GetAiInsight(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _aiLeadAssistantService.GetInsightAsync(id, cancellationToken));
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
        await _createValidator.ValidateAndThrowAppExceptionAsync(request, cancellationToken);
        var lead = await _leadService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = lead.Id }, lead);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<LeadDto>> Update(Guid id, UpdateLeadRequest request, CancellationToken cancellationToken)
    {
        await _updateValidator.ValidateAndThrowAppExceptionAsync(request, cancellationToken);
        return Ok(await _leadService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _leadService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/assign")]
    public async Task<ActionResult<LeadDto>> Assign(Guid id, AssignLeadRequest request, CancellationToken cancellationToken)
    {
        await _assignValidator.ValidateAndThrowAppExceptionAsync(request, cancellationToken);
        return Ok(await _leadService.AssignAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/transfer")]
    public async Task<ActionResult<LeadDto>> Transfer(Guid id, AssignLeadRequest request, CancellationToken cancellationToken)
    {
        await _assignValidator.ValidateAndThrowAppExceptionAsync(request, cancellationToken);
        return Ok(await _leadService.TransferAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/activities")]
    public async Task<ActionResult<LeadActivityDto>> AddActivity(Guid id, CreateLeadActivityRequest request, CancellationToken cancellationToken)
    {
        await _activityValidator.ValidateAndThrowAppExceptionAsync(request, cancellationToken);
        return Ok(await _leadActivityService.AddActivityAsync(id, request, cancellationToken));
    }

    [HttpGet("{id:guid}/activities")]
    public async Task<ActionResult<IReadOnlyList<LeadActivityDto>>> GetTimeline(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _leadActivityService.GetTimelineAsync(id, cancellationToken));
    }

    [HttpGet("follow-ups/upcoming")]
    public async Task<ActionResult<IReadOnlyList<LeadActivityDto>>> GetUpcomingFollowUps(
        [FromQuery] int days, CancellationToken cancellationToken)
    {
        return Ok(await _leadActivityService.GetUpcomingFollowUpsAsync(days <= 0 ? 7 : days, cancellationToken));
    }
}
