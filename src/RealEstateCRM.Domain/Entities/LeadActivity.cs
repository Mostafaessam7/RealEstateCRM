using RealEstateCRM.Domain.Common;
using RealEstateCRM.Domain.Enums;

namespace RealEstateCRM.Domain.Entities;

/// <summary>An immutable timeline entry for a Lead. No UpdatedAt — activities are not edited.</summary>
public class LeadActivity : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid LeadId { get; set; }
    public Guid UserId { get; set; }
    public LeadActivityType Type { get; set; }
    public string? Description { get; set; }

    /// <summary>When the activity happened, or — for Type == FollowUp — when it's scheduled for.</summary>
    public DateTime ActivityDate { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>Set once the follow-up reminder job has notified the assigned agent. Prevents duplicate reminders.</summary>
    public DateTime? ReminderSentAt { get; set; }
}
