using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateCRM.Application.Common.Validation;
using RealEstateCRM.Application.Webhooks;
using RealEstateCRM.Application.Webhooks.DTOs;
using RealEstateCRM.Domain.Constants;

namespace RealEstateCRM.Api.Controllers;

[ApiController]
[Authorize(Roles = $"{Roles.CompanyAdmin},{Roles.SuperAdmin}")]
[Route("api/webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly IWebhookService _webhookService;
    private readonly IValidator<CreateWebhookSubscriptionRequest> _createValidator;

    public WebhooksController(IWebhookService webhookService, IValidator<CreateWebhookSubscriptionRequest> createValidator)
    {
        _webhookService = webhookService;
        _createValidator = createValidator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WebhookSubscriptionDto>>> List(CancellationToken cancellationToken)
    {
        return Ok(await _webhookService.ListAsync(cancellationToken));
    }

    [HttpGet("event-types")]
    public ActionResult<IReadOnlyList<string>> ListEventTypes() => Ok(WebhookEventTypes.All);

    [HttpPost]
    public async Task<ActionResult<CreatedWebhookSubscriptionDto>> Create(CreateWebhookSubscriptionRequest request, CancellationToken cancellationToken)
    {
        await _createValidator.ValidateAndThrowAppExceptionAsync(request, cancellationToken);
        return Ok(await _webhookService.CreateAsync(request, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _webhookService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/deliveries")]
    public async Task<ActionResult<IReadOnlyList<WebhookDeliveryDto>>> ListDeliveries(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _webhookService.ListDeliveriesAsync(id, cancellationToken));
    }
}
