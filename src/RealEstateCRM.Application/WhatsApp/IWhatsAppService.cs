using RealEstateCRM.Application.WhatsApp.DTOs;

namespace RealEstateCRM.Application.WhatsApp;

public interface IWhatsAppService
{
    Task<IReadOnlyList<WhatsAppTemplateDto>> ListTemplatesAsync(CancellationToken cancellationToken = default);

    Task<WhatsAppTemplateDto> CreateTemplateAsync(CreateWhatsAppTemplateRequest request, CancellationToken cancellationToken = default);

    Task<WhatsAppTemplateDto> UpdateTemplateAsync(Guid id, UpdateWhatsAppTemplateRequest request, CancellationToken cancellationToken = default);

    /// <summary>Renders (if TemplateId given) or uses the raw Body, sends via IWhatsAppSender, and logs the result.</summary>
    Task<WhatsAppMessageDto> SendToLeadAsync(Guid leadId, SendWhatsAppRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WhatsAppMessageDto>> ListMessagesAsync(Guid leadId, CancellationToken cancellationToken = default);
}
