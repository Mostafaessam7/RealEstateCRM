using Microsoft.EntityFrameworkCore;
using RealEstateCRM.Application.Common.Exceptions;
using RealEstateCRM.Application.Common.Interfaces;
using RealEstateCRM.Application.WhatsApp;
using RealEstateCRM.Application.WhatsApp.DTOs;
using RealEstateCRM.Domain.Constants;
using RealEstateCRM.Domain.Entities;
using RealEstateCRM.Domain.Enums;
using RealEstateCRM.Infrastructure.Persistence;

namespace RealEstateCRM.Infrastructure.WhatsApp;

public class WhatsAppService : IWhatsAppService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantService _currentTenant;
    private readonly IWhatsAppSender _sender;

    public WhatsAppService(ApplicationDbContext db, ICurrentTenantService currentTenant, IWhatsAppSender sender)
    {
        _db = db;
        _currentTenant = currentTenant;
        _sender = sender;
    }

    public async Task<IReadOnlyList<WhatsAppTemplateDto>> ListTemplatesAsync(CancellationToken cancellationToken = default)
    {
        var templates = await _db.WhatsAppTemplates.AsNoTracking()
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);

        return templates.Select(ToDto).ToList();
    }

    public async Task<WhatsAppTemplateDto> CreateTemplateAsync(CreateWhatsAppTemplateRequest request, CancellationToken cancellationToken = default)
    {
        EnsureElevatedAccess();

        var template = new WhatsAppTemplate
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Body = request.Body,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.WhatsAppTemplates.Add(template);
        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(template);
    }

    public async Task<WhatsAppTemplateDto> UpdateTemplateAsync(Guid id, UpdateWhatsAppTemplateRequest request, CancellationToken cancellationToken = default)
    {
        EnsureElevatedAccess();

        var template = await _db.WhatsAppTemplates.FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new AppException("Template not found.", 404);

        template.Name = request.Name;
        template.Body = request.Body;
        template.IsActive = request.IsActive;
        template.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(template);
    }

    public async Task<WhatsAppMessageDto> SendToLeadAsync(Guid leadId, SendWhatsAppRequest request, CancellationToken cancellationToken = default)
    {
        var userId = _currentTenant.UserId ?? throw new AppException("Authenticated user context is required.", 401);

        var lead = await _db.Leads.FirstOrDefaultAsync(l => l.Id == leadId, cancellationToken)
            ?? throw new AppException("Lead not found.", 404);

        if (string.IsNullOrWhiteSpace(lead.Phone))
        {
            throw new AppException("Lead has no phone number on file.", 400);
        }

        string body;
        Guid? templateId = null;

        if (request.TemplateId.HasValue)
        {
            var template = await _db.WhatsAppTemplates.FirstOrDefaultAsync(t => t.Id == request.TemplateId.Value, cancellationToken)
                ?? throw new AppException("Template not found.", 404);

            body = RenderTemplate(template.Body, lead);
            templateId = template.Id;
        }
        else
        {
            body = request.Body!;
        }

        var message = new WhatsAppMessage
        {
            Id = Guid.NewGuid(),
            LeadId = lead.Id,
            SentByUserId = userId,
            TemplateId = templateId,
            ToPhone = lead.Phone,
            Body = body,
            Status = WhatsAppMessageStatus.Queued,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            var accepted = await _sender.SendAsync(lead.Phone, body, cancellationToken);
            message.Status = accepted ? WhatsAppMessageStatus.Sent : WhatsAppMessageStatus.Failed;
            message.SentAt = accepted ? DateTime.UtcNow : null;
            message.ErrorMessage = accepted ? null : "The WhatsApp provider rejected the message.";
        }
        catch (Exception ex)
        {
            message.Status = WhatsAppMessageStatus.Failed;
            message.ErrorMessage = ex.Message;
        }

        _db.WhatsAppMessages.Add(message);
        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(message);
    }

    public async Task<IReadOnlyList<WhatsAppMessageDto>> ListMessagesAsync(Guid leadId, CancellationToken cancellationToken = default)
    {
        var messages = await _db.WhatsAppMessages.AsNoTracking()
            .Where(m => m.LeadId == leadId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);

        return messages.Select(ToDto).ToList();
    }

    private static string RenderTemplate(string body, Lead lead) => body
        .Replace("{{FullName}}", lead.FullName)
        .Replace("{{PreferredLocation}}", lead.PreferredLocation ?? string.Empty)
        .Replace("{{PropertyType}}", lead.PropertyType ?? string.Empty);

    private void EnsureElevatedAccess()
    {
        var allowed = _currentTenant.IsSuperAdmin ||
            _currentTenant.IsInRole(Roles.CompanyAdmin) ||
            _currentTenant.IsInRole(Roles.SalesManager);

        if (!allowed)
        {
            throw new AppException("You are not authorized to manage WhatsApp templates.", 403);
        }
    }

    private static WhatsAppTemplateDto ToDto(WhatsAppTemplate template) => new()
    {
        Id = template.Id,
        Name = template.Name,
        Body = template.Body,
        IsActive = template.IsActive
    };

    private static WhatsAppMessageDto ToDto(WhatsAppMessage message) => new()
    {
        Id = message.Id,
        LeadId = message.LeadId,
        TemplateId = message.TemplateId,
        ToPhone = message.ToPhone,
        Body = message.Body,
        Status = message.Status,
        ErrorMessage = message.ErrorMessage,
        SentAt = message.SentAt,
        CreatedAt = message.CreatedAt
    };
}
