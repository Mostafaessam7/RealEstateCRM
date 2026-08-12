using RealEstateCRM.Domain.Common;
using RealEstateCRM.Domain.Enums;

namespace RealEstateCRM.Domain.Entities;

/// <summary>
/// A one-shot bulk broadcast to a segment of Leads (filtered by Status/Source), sent over
/// Email or WhatsApp. Not a scheduled/recurring drip sequence — that's a natural next step.
/// </summary>
public class Campaign : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public CampaignChannel Channel { get; set; }
    public string? Subject { get; set; }
    public string Body { get; set; } = string.Empty;
    public LeadStatus? TargetStatus { get; set; }
    public LeadSource? TargetSource { get; set; }
    public CampaignStatus Status { get; set; } = CampaignStatus.Draft;
    public Guid CreatedByUserId { get; set; }
    public DateTime? SentAt { get; set; }
    public int RecipientCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
}
