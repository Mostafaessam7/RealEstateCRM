namespace RealEstateCRM.Application.Recommendations.DTOs;

public class UnitRecommendationDto
{
    public Guid UnitId { get; set; }
    public Guid ProjectId { get; set; }
    public string UnitCode { get; set; } = string.Empty;
    public string? PropertyType { get; set; }
    public decimal Price { get; set; }
    public string? Location { get; set; }
    public int Score { get; set; }
    public List<string> MatchReasons { get; set; } = new();

    /// <summary>
    /// ML-predicted probability [0,1] this lead converts on this unit, learned from the
    /// company's own historical Contracted/Cancelled deals. Null when there isn't enough
    /// history yet to train a model (falls back to the rule-based Score alone).
    /// </summary>
    public float? ConversionLikelihood { get; set; }
}
