using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.ML.Data;
using RealEstateCRM.Domain.Entities;
using RealEstateCRM.Domain.Enums;
using RealEstateCRM.Infrastructure.Persistence;

namespace RealEstateCRM.Infrastructure.Recommendations;

/// <summary>
/// The "advanced" (ML.NET, not rule-based) half of the recommendation engine. Trains a small
/// binary classifier per company from its own historical Contracted vs. Cancelled deals,
/// predicting conversion likelihood for a Lead/Unit pair. Falls back to null (pure rule-based
/// scoring) when a company doesn't have enough resolved deals yet to train a meaningful model —
/// no fabricated confidence from too little data.
///
/// Trains from each Deal's FeatureSnapshot* columns (captured once, at deal-creation time by
/// DealService) rather than the Lead's/Unit's current field values — this is what makes the
/// training signal accurate: it reflects the match as it actually was when the deal was made,
/// not however those records have drifted since. Deals created before the snapshot columns
/// existed have null snapshots and are excluded from training (nothing to backfill — that
/// historical state isn't recoverable).
/// </summary>
public class MlConversionScorer
{
    private const int MinTrainingDeals = 10;
    private static readonly TimeSpan ModelCacheTtl = TimeSpan.FromMinutes(15);

    private static readonly ConcurrentDictionary<Guid, (DateTime TrainedAt, ITransformer Model, MLContext Context)> ModelCache = new();

    private readonly ApplicationDbContext _db;

    public MlConversionScorer(ApplicationDbContext db)
    {
        _db = db;
    }

    public class DealFeatures
    {
        public float BudgetFit { get; set; }
        public float LocationMatch { get; set; }
        public float PropertyTypeMatch { get; set; }
        public float PriceToBudgetRatio { get; set; }
        public bool Converted { get; set; }
    }

    private class ConversionPrediction
    {
        [ColumnName("PredictedLabel")]
        public bool Converted { get; set; }
        public float Probability { get; set; }
    }

    /// <summary>Returns UnitId -> conversion probability [0,1], or null when there's insufficient training data.</summary>
    public async Task<IReadOnlyDictionary<Guid, float>?> TryScoreAsync(Guid companyId, Lead lead, IReadOnlyList<Unit> candidateUnits, CancellationToken cancellationToken)
    {
        var model = await GetOrTrainModelAsync(companyId, cancellationToken);
        if (model is null)
        {
            return null;
        }

        var (context, transformer) = model.Value;
        var predictionEngine = context.Model.CreatePredictionEngine<DealFeatures, ConversionPrediction>(transformer);

        var result = new Dictionary<Guid, float>();
        foreach (var unit in candidateUnits)
        {
            var features = LeadUnitFeatureCalculator.Compute(lead, unit);
            var prediction = predictionEngine.Predict(new DealFeatures
            {
                BudgetFit = features.BudgetFit,
                LocationMatch = features.LocationMatch,
                PropertyTypeMatch = features.PropertyTypeMatch,
                PriceToBudgetRatio = features.PriceToBudgetRatio
            });
            result[unit.Id] = prediction.Probability;
        }

        return result;
    }

    private async Task<(MLContext Context, ITransformer Transformer)?> GetOrTrainModelAsync(Guid companyId, CancellationToken cancellationToken)
    {
        if (ModelCache.TryGetValue(companyId, out var cached) && DateTime.UtcNow - cached.TrainedAt < ModelCacheTtl)
        {
            return (cached.Context, cached.Model);
        }

        var trainingRows = await _db.Deals.AsNoTracking()
            .Where(d => d.CompanyId == companyId
                && (d.Status == DealStatus.Contracted || d.Status == DealStatus.Cancelled)
                && d.FeatureSnapshotBudgetFit != null
                && d.FeatureSnapshotLocationMatch != null
                && d.FeatureSnapshotPropertyTypeMatch != null
                && d.FeatureSnapshotPriceToBudgetRatio != null)
            .Select(d => new DealFeatures
            {
                BudgetFit = d.FeatureSnapshotBudgetFit!.Value,
                LocationMatch = d.FeatureSnapshotLocationMatch!.Value,
                PropertyTypeMatch = d.FeatureSnapshotPropertyTypeMatch!.Value,
                PriceToBudgetRatio = d.FeatureSnapshotPriceToBudgetRatio!.Value,
                Converted = d.Status == DealStatus.Contracted
            })
            .ToListAsync(cancellationToken);

        if (trainingRows.Count < MinTrainingDeals)
        {
            return null;
        }

        var context = new MLContext(seed: 42);
        var trainingData = context.Data.LoadFromEnumerable(trainingRows);

        var pipeline = context.Transforms.Concatenate("Features",
                nameof(DealFeatures.BudgetFit), nameof(DealFeatures.LocationMatch),
                nameof(DealFeatures.PropertyTypeMatch), nameof(DealFeatures.PriceToBudgetRatio))
            .Append(context.BinaryClassification.Trainers.SdcaLogisticRegression(labelColumnName: nameof(DealFeatures.Converted)));

        var transformer = pipeline.Fit(trainingData);

        ModelCache[companyId] = (DateTime.UtcNow, transformer, context);
        return (context, transformer);
    }
}
