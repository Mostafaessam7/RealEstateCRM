using RealEstateCRM.Application.AiAssistant.DTOs;

namespace RealEstateCRM.Application.AiAssistant;

public interface IAiLeadAssistantService
{
    /// <summary>Generates a summary, a next-best-action, and a draft follow-up message for a Lead.</summary>
    Task<AiLeadInsightDto> GetInsightAsync(Guid leadId, CancellationToken cancellationToken = default);
}
