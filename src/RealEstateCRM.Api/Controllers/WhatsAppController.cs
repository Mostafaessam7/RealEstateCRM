using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateCRM.Application.Common.Validation;
using RealEstateCRM.Application.WhatsApp;
using RealEstateCRM.Application.WhatsApp.DTOs;

namespace RealEstateCRM.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/whatsapp")]
public class WhatsAppController : ControllerBase
{
    private readonly IWhatsAppService _whatsAppService;
    private readonly IValidator<CreateWhatsAppTemplateRequest> _createTemplateValidator;
    private readonly IValidator<UpdateWhatsAppTemplateRequest> _updateTemplateValidator;
    private readonly IValidator<SendWhatsAppRequest> _sendValidator;

    public WhatsAppController(
        IWhatsAppService whatsAppService,
        IValidator<CreateWhatsAppTemplateRequest> createTemplateValidator,
        IValidator<UpdateWhatsAppTemplateRequest> updateTemplateValidator,
        IValidator<SendWhatsAppRequest> sendValidator)
    {
        _whatsAppService = whatsAppService;
        _createTemplateValidator = createTemplateValidator;
        _updateTemplateValidator = updateTemplateValidator;
        _sendValidator = sendValidator;
    }

    [HttpGet("templates")]
    public async Task<ActionResult<IReadOnlyList<WhatsAppTemplateDto>>> ListTemplates(CancellationToken cancellationToken)
    {
        return Ok(await _whatsAppService.ListTemplatesAsync(cancellationToken));
    }

    [HttpPost("templates")]
    public async Task<ActionResult<WhatsAppTemplateDto>> CreateTemplate(CreateWhatsAppTemplateRequest request, CancellationToken cancellationToken)
    {
        await _createTemplateValidator.ValidateAndThrowAppExceptionAsync(request, cancellationToken);
        return Ok(await _whatsAppService.CreateTemplateAsync(request, cancellationToken));
    }

    [HttpPut("templates/{id:guid}")]
    public async Task<ActionResult<WhatsAppTemplateDto>> UpdateTemplate(Guid id, UpdateWhatsAppTemplateRequest request, CancellationToken cancellationToken)
    {
        await _updateTemplateValidator.ValidateAndThrowAppExceptionAsync(request, cancellationToken);
        return Ok(await _whatsAppService.UpdateTemplateAsync(id, request, cancellationToken));
    }

    [HttpGet("leads/{leadId:guid}/messages")]
    public async Task<ActionResult<IReadOnlyList<WhatsAppMessageDto>>> ListMessages(Guid leadId, CancellationToken cancellationToken)
    {
        return Ok(await _whatsAppService.ListMessagesAsync(leadId, cancellationToken));
    }

    [HttpPost("leads/{leadId:guid}/send")]
    public async Task<ActionResult<WhatsAppMessageDto>> Send(Guid leadId, SendWhatsAppRequest request, CancellationToken cancellationToken)
    {
        await _sendValidator.ValidateAndThrowAppExceptionAsync(request, cancellationToken);
        return Ok(await _whatsAppService.SendToLeadAsync(leadId, request, cancellationToken));
    }
}
