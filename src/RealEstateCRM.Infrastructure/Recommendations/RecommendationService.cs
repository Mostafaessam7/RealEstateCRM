using Microsoft.EntityFrameworkCore;
using RealEstateCRM.Application.Common.Exceptions;
using RealEstateCRM.Application.Common.Interfaces;
using RealEstateCRM.Application.Recommendations;
using RealEstateCRM.Application.Recommendations.DTOs;
using RealEstateCRM.Domain.Entities;
using RealEstateCRM.Domain.Enums;
using RealEstateCRM.Infrastructure.Persistence;

namespace RealEstateCRM.Infrastructure.Recommendations;

public class RecommendationService : IRecommendationService
{
    private const int BudgetFitScore = 40;
    private const int BudgetNearScore = 20;
    private const int LocationMatchScore = 30;
    private const int PropertyTypeMatchScore = 30;

    /// <summary>A unit priced within this fraction outside the lead's budget still counts as a near-fit.</summary>
    private const decimal NearBudgetTolerance = 0.15m;

    /// <summary>Weight given to the rule-based score vs. the ML conversion likelihood once both are available.</summary>
    private const double RuleBasedWeight = 0.6;

    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantService? _currentTenant;
    private readonly MlConversionScorer _mlScorer;

    public RecommendationService(ApplicationDbContext db, ICurrentTenantService? currentTenant = null)
    {
        _db = db;
        _currentTenant = currentTenant;
        _mlScorer = new MlConversionScorer(db);
    }

    public async Task<IReadOnlyList<UnitRecommendationDto>> GetRecommendationsForLeadAsync(Guid leadId, int count = 5, CancellationToken cancellationToken = default)
    {
        var lead = await _db.Leads.AsNoTracking().FirstOrDefaultAsync(l => l.Id == leadId, cancellationToken)
            ?? throw new AppException("Lead not found.", 404);

        var take = count is < 1 or > 50 ? 5 : count;

        var units = await _db.Units.AsNoTracking()
            .Where(u => u.Status == UnitStatus.Available)
            .ToListAsync(cancellationToken);

        var scored = units.Select(u => Score(lead, u)).ToList();

        var companyId = _currentTenant?.CompanyId ?? lead.CompanyId;
        var mlScores = await _mlScorer.TryScoreAsync(companyId, lead, units, cancellationToken);
        if (mlScores is not null)
        {
            foreach (var item in scored)
            {
                if (mlScores.TryGetValue(item.UnitId, out var likelihood))
                {
                    item.ConversionLikelihood = likelihood;
                    item.Score = (int)Math.Round(item.Score * RuleBasedWeight + likelihood * 100 * (1 - RuleBasedWeight));
                }
            }
        }

        return scored
            .OrderByDescending(r => r.Score)
            .ThenBy(r => r.Price)
            .Take(take)
            .ToList();
    }

    private static UnitRecommendationDto Score(Lead lead, Unit unit)
    {
        var score = 0;
        var reasons = new List<string>();

        if (lead.BudgetMin.HasValue || lead.BudgetMax.HasValue)
        {
            var min = lead.BudgetMin ?? 0;
            var max = lead.BudgetMax ?? decimal.MaxValue;

            if (unit.Price >= min && unit.Price <= max)
            {
                score += BudgetFitScore;
                reasons.Add("Within budget");
            }
            else
            {
                var nearMin = min * (1 - NearBudgetTolerance);
                var nearMax = max == decimal.MaxValue ? decimal.MaxValue : max * (1 + NearBudgetTolerance);
                if (unit.Price >= nearMin && unit.Price <= nearMax)
                {
                    score += BudgetNearScore;
                    reasons.Add("Close to budget");
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(lead.PreferredLocation) &&
            !string.IsNullOrWhiteSpace(unit.Location) &&
            unit.Location.Contains(lead.PreferredLocation, StringComparison.OrdinalIgnoreCase))
        {
            score += LocationMatchScore;
            reasons.Add("Preferred location");
        }

        if (!string.IsNullOrWhiteSpace(lead.PropertyType) &&
            !string.IsNullOrWhiteSpace(unit.PropertyType) &&
            string.Equals(lead.PropertyType, unit.PropertyType, StringComparison.OrdinalIgnoreCase))
        {
            score += PropertyTypeMatchScore;
            reasons.Add("Matches property type");
        }

        return new UnitRecommendationDto
        {
            UnitId = unit.Id,
            ProjectId = unit.ProjectId,
            UnitCode = unit.UnitCode,
            PropertyType = unit.PropertyType,
            Price = unit.Price,
            Location = unit.Location,
            Score = score,
            MatchReasons = reasons
        };
    }
}
