using RealEstateCRM.Domain.Entities;

namespace RealEstateCRM.Infrastructure.Recommendations;

/// <summary>
/// The shared match-feature calculation between a Lead's stated preferences and a Unit —
/// used both to snapshot a Deal's features at creation time (DealService) and to score
/// candidate units for a lead at recommendation time (MlConversionScorer). Kept as one
/// static calculation so both call sites can never drift apart.
/// </summary>
public static class LeadUnitFeatureCalculator
{
    public record Features(float BudgetFit, float LocationMatch, float PropertyTypeMatch, float PriceToBudgetRatio);

    public static Features Compute(Lead lead, Unit unit)
    {
        var budgetFit = 0f;
        if (lead.BudgetMin.HasValue || lead.BudgetMax.HasValue)
        {
            var min = lead.BudgetMin ?? 0;
            var max = lead.BudgetMax ?? decimal.MaxValue;
            budgetFit = unit.Price >= min && unit.Price <= max ? 1f : 0f;
        }

        var locationMatch = !string.IsNullOrWhiteSpace(lead.PreferredLocation) &&
            !string.IsNullOrWhiteSpace(unit.Location) &&
            unit.Location.Contains(lead.PreferredLocation, StringComparison.OrdinalIgnoreCase)
                ? 1f : 0f;

        var propertyTypeMatch = !string.IsNullOrWhiteSpace(lead.PropertyType) &&
            !string.IsNullOrWhiteSpace(unit.PropertyType) &&
            string.Equals(lead.PropertyType, unit.PropertyType, StringComparison.OrdinalIgnoreCase)
                ? 1f : 0f;

        var midBudget = lead.BudgetMax ?? lead.BudgetMin;
        var priceRatio = midBudget is > 0 ? (float)(unit.Price / midBudget.Value) : 1f;

        return new Features(budgetFit, locationMatch, propertyTypeMatch, priceRatio);
    }
}
