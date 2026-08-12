using RealEstateCRM.Domain.Common;
using RealEstateCRM.Domain.Enums;

namespace RealEstateCRM.Domain.Entities;

/// <summary>An outbound WhatsApp message logged against a Lead. No UpdatedAt — immutable once sent/failed.</summary>
public class WhatsAppMessage : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid LeadId { get; set; }
    public Guid SentByUserId { get; set; }
    public Guid? TemplateId { get; set; }
    public string ToPhone { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public WhatsAppMessageStatus Status { get; set; } = WhatsAppMessageStatus.Queued;
    public string? ErrorMessage { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
