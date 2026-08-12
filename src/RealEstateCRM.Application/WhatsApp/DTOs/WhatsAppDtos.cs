using RealEstateCRM.Domain.Enums;

namespace RealEstateCRM.Application.WhatsApp.DTOs;

public class WhatsAppTemplateDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class CreateWhatsAppTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}

public class UpdateWhatsAppTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class WhatsAppMessageDto
{
    public Guid Id { get; set; }
    public Guid LeadId { get; set; }
    public Guid? TemplateId { get; set; }
    public string ToPhone { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public WhatsAppMessageStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Either TemplateId (rendered against the lead) or a raw Body must be provided.</summary>
public class SendWhatsAppRequest
{
    public Guid? TemplateId { get; set; }
    public string? Body { get; set; }
}
