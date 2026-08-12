using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateCRM.Application.Common.Validation;
using RealEstateCRM.Application.Subscriptions;
using RealEstateCRM.Application.Subscriptions.DTOs;
using RealEstateCRM.Domain.Constants;

namespace RealEstateCRM.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/subscriptions")]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly ISubscriptionPlanService _planService;
    private readonly IValidator<ChangePlanRequest> _changePlanValidator;
    private readonly IValidator<CreateSubscriptionPlanRequest> _createPlanValidator;
    private readonly IValidator<UpdateSubscriptionPlanRequest> _updatePlanValidator;

    public SubscriptionsController(
        ISubscriptionService subscriptionService,
        ISubscriptionPlanService planService,
        IValidator<ChangePlanRequest> changePlanValidator,
        IValidator<CreateSubscriptionPlanRequest> createPlanValidator,
        IValidator<UpdateSubscriptionPlanRequest> updatePlanValidator)
    {
        _subscriptionService = subscriptionService;
        _planService = planService;
        _changePlanValidator = changePlanValidator;
        _createPlanValidator = createPlanValidator;
        _updatePlanValidator = updatePlanValidator;
    }

    [HttpGet("plans")]
    public async Task<ActionResult<IReadOnlyList<SubscriptionPlanDto>>> ListPlans(CancellationToken cancellationToken)
    {
        return Ok(await _planService.ListAsync(activeOnly: true, cancellationToken));
    }

    [HttpGet("current")]
    public async Task<ActionResult<CompanySubscriptionDto>> GetCurrent(CancellationToken cancellationToken)
    {
        return Ok(await _subscriptionService.GetCurrentAsync(cancellationToken));
    }

    [HttpPost("change-plan")]
    public async Task<ActionResult<CompanySubscriptionDto>> ChangePlan(ChangePlanRequest request, CancellationToken cancellationToken)
    {
        await _changePlanValidator.ValidateAndThrowAppExceptionAsync(request, cancellationToken);
        return Ok(await _subscriptionService.ChangePlanAsync(request, cancellationToken));
    }

    [HttpPost("cancel")]
    public async Task<ActionResult<CompanySubscriptionDto>> Cancel(CancellationToken cancellationToken)
    {
        return Ok(await _subscriptionService.CancelAsync(cancellationToken));
    }

    [HttpGet("plans/all")]
    [Authorize(Roles = Roles.SuperAdmin)]
    public async Task<ActionResult<IReadOnlyList<SubscriptionPlanDto>>> ListAllPlans(CancellationToken cancellationToken)
    {
        return Ok(await _planService.ListAsync(activeOnly: false, cancellationToken));
    }

    [HttpPost("plans")]
    [Authorize(Roles = Roles.SuperAdmin)]
    public async Task<ActionResult<SubscriptionPlanDto>> CreatePlan(CreateSubscriptionPlanRequest request, CancellationToken cancellationToken)
    {
        await _createPlanValidator.ValidateAndThrowAppExceptionAsync(request, cancellationToken);
        var plan = await _planService.CreateAsync(request, cancellationToken);
        return Ok(plan);
    }

    [HttpPut("plans/{id:guid}")]
    [Authorize(Roles = Roles.SuperAdmin)]
    public async Task<ActionResult<SubscriptionPlanDto>> UpdatePlan(Guid id, UpdateSubscriptionPlanRequest request, CancellationToken cancellationToken)
    {
        await _updatePlanValidator.ValidateAndThrowAppExceptionAsync(request, cancellationToken);
        return Ok(await _planService.UpdateAsync(id, request, cancellationToken));
    }
}
