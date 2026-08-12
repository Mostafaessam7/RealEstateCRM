namespace RealEstateCRM.Application.AiAssistant.DTOs;

public class AiLeadInsightDto
{
    public string Summary { get; set; } = string.Empty;
    public string NextBestAction { get; set; } = string.Empty;
    public string SuggestedFollowUpMessage { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
}
