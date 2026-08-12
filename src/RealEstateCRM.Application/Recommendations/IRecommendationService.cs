using RealEstateCRM.Application.Recommendations.DTOs;

namespace RealEstateCRM.Application.Recommendations;

public interface IRecommendationService
{
    /// <summary>
    /// Rule-based matching of Available units against a Lead's budget/location/property-type
    /// preferences. Not machine learning — a deterministic, explainable score.
    /// </summary>
    Task<IReadOnlyList<UnitRecommendationDto>> GetRecommendationsForLeadAsync(Guid leadId, int count = 5, CancellationToken cancellationToken = default);
}
