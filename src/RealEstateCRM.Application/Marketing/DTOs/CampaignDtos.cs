using RealEstateCRM.Domain.Enums;

namespace RealEstateCRM.Application.Marketing.DTOs;

public class CampaignDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public CampaignChannel Channel { get; set; }
    public string? Subject { get; set; }
    public string Body { get; set; } = string.Empty;
    public LeadStatus? TargetStatus { get; set; }
    public LeadSource? TargetSource { get; set; }
    public CampaignStatus Status { get; set; }
    public DateTime? SentAt { get; set; }
    public int RecipientCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateCampaignRequest
{
    public string Name { get; set; } = string.Empty;
    public CampaignChannel Channel { get; set; }
    public string? Subject { get; set; }
    public string Body { get; set; } = string.Empty;
    public LeadStatus? TargetStatus { get; set; }
    public LeadSource? TargetSource { get; set; }
}

public class CampaignRecipientDto
{
    public Guid Id { get; set; }
    public Guid LeadId { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime SentAt { get; set; }
}
