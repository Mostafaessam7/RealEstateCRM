using Microsoft.EntityFrameworkCore;
using RealEstateCRM.Application.AiAssistant;
using RealEstateCRM.Application.AiAssistant.DTOs;
using RealEstateCRM.Application.Common.Exceptions;
using RealEstateCRM.Domain.Entities;
using RealEstateCRM.Domain.Enums;
using RealEstateCRM.Infrastructure.Persistence;

namespace RealEstateCRM.Infrastructure.AiAssistant;

/// <summary>
/// Rule-based IAiLeadAssistantService — no external LLM API key is configured for this
/// deployment, so insights are generated from deterministic heuristics over the lead's own
/// data (status, source, budget, recency of contact) rather than a hosted model. The interface
/// is the extension point: swap in a real LLM-backed implementation (behind an API key read
/// from configuration, never hardcoded) without touching callers.
/// </summary>
public class TemplateAiLeadAssistantService : IAiLeadAssistantService
{
    private static readonly Dictionary<LeadStatus, string> NextBestActionByStatus = new()
    {
        [LeadStatus.New] = "Reach out within 24 hours to qualify the lead's needs and budget.",
        [LeadStatus.Contacted] = "Schedule a discovery call to understand requirements in detail.",
        [LeadStatus.Interested] = "Share matching unit options and arrange a viewing.",
        [LeadStatus.Viewing] = "Follow up after the viewing for feedback and next steps.",
        [LeadStatus.Negotiation] = "Prepare a tailored offer or payment plan to move the deal forward.",
        [LeadStatus.Reserved] = "Confirm reservation details and guide the lead toward contract signing.",
        [LeadStatus.Contracted] = "Ensure handover and paperwork are on track — no further sales action needed.",
        [LeadStatus.Lost] = "Re-engage in a few months, or move to a long-term nurture list."
    };

    private readonly ApplicationDbContext _db;

    public TemplateAiLeadAssistantService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<AiLeadInsightDto> GetInsightAsync(Guid leadId, CancellationToken cancellationToken = default)
    {
        var lead = await _db.Leads.AsNoTracking().FirstOrDefaultAsync(l => l.Id == leadId, cancellationToken)
            ?? throw new AppException("Lead not found.", 404);

        var activityCount = await _db.LeadActivities.AsNoTracking().CountAsync(a => a.LeadId == leadId, cancellationToken);
        var lastActivityDate = await _db.LeadActivities.AsNoTracking()
            .Where(a => a.LeadId == leadId)
            .OrderByDescending(a => a.ActivityDate)
            .Select(a => (DateTime?)a.ActivityDate)
            .FirstOrDefaultAsync(cancellationToken);

        var daysSinceContact = lastActivityDate.HasValue ? (DateTime.UtcNow - lastActivityDate.Value).Days : (int?)null;

        return new AiLeadInsightDto
        {
            Summary = BuildSummary(lead, activityCount, daysSinceContact),
            NextBestAction = BuildNextBestAction(lead.Status, daysSinceContact),
            SuggestedFollowUpMessage = BuildFollowUpMessage(lead),
            GeneratedAt = DateTime.UtcNow
        };
    }

    private static string BuildSummary(Lead lead, int activityCount, int? daysSinceContact)
    {
        var budget = lead.BudgetMin.HasValue || lead.BudgetMax.HasValue
            ? $" with a budget of {FormatBudget(lead.BudgetMin, lead.BudgetMax)}"
            : string.Empty;

        var interest = !string.IsNullOrWhiteSpace(lead.PropertyType) || !string.IsNullOrWhiteSpace(lead.PreferredLocation)
            ? $", interested in {lead.PropertyType ?? "a property"} in {lead.PreferredLocation ?? "an unspecified location"}"
            : string.Empty;

        var contact = daysSinceContact.HasValue
            ? $" Last contact was {daysSinceContact.Value} day(s) ago across {activityCount} touchpoint(s)."
            : " No activity has been logged for this lead yet.";

        return $"{lead.FullName} is a {lead.Status} lead sourced from {lead.Source}{budget}{interest}.{contact}";
    }

    private static string BuildNextBestAction(LeadStatus status, int? daysSinceContact)
    {
        var action = NextBestActionByStatus.GetValueOrDefault(status, "Review the lead and decide the next step.");

        if (status != LeadStatus.Contracted && status != LeadStatus.Lost && daysSinceContact is > 7)
        {
            return $"This lead has gone quiet for {daysSinceContact} days — prioritize it. {action}";
        }

        return action;
    }

    private static string BuildFollowUpMessage(Lead lead)
    {
        var focus = !string.IsNullOrWhiteSpace(lead.PropertyType) && !string.IsNullOrWhiteSpace(lead.PreferredLocation)
            ? $"{lead.PropertyType} properties in {lead.PreferredLocation}"
            : !string.IsNullOrWhiteSpace(lead.PreferredLocation)
                ? $"properties in {lead.PreferredLocation}"
                : "properties that match your needs";

        return $"Hi {lead.FullName}, following up on your interest in {focus}. Do you have time this week for a quick call or a viewing?";
    }

    private static string FormatBudget(decimal? min, decimal? max)
    {
        if (min.HasValue && max.HasValue) return $"{min:N0}–{max:N0}";
        if (min.HasValue) return $"{min:N0}+";
        return $"up to {max:N0}";
    }
}
