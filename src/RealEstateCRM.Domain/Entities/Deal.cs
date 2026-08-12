using RealEstateCRM.Domain.Common;
using RealEstateCRM.Domain.Enums;

namespace RealEstateCRM.Domain.Entities;

/// <summary>
/// No soft delete — deals are business history and are never removed. Cancellation is a
/// terminal status (DealStatus.Cancelled), not a delete. See docs/database.md#deal.
/// </summary>
public class Deal : TenantEntity
{
    public Guid LeadId { get; set; }
    public Guid UnitId { get; set; }
    public Guid SalesAgentId { get; set; }
    public decimal DealValue { get; set; }
    public DealStatus Status { get; set; } = DealStatus.Pending;
    public DateTime? ReservationDate { get; set; }
    public DateTime? ContractDate { get; set; }
    public string? Notes { get; set; }

    /// <summary>
    /// Lead/Unit match features captured at deal-creation time — how well this unit matched
    /// the lead's stated preferences at the moment the deal was made, not whatever the lead's
    /// or unit's fields happen to say later. Used to train the ML conversion-likelihood model
    /// (RecommendationService) on accurate historical signal instead of current-state drift.
    /// Null for deals created before this was added — never backfilled, since the accurate
    /// historical state doesn't exist to reconstruct.
    /// </summary>
    public float? FeatureSnapshotBudgetFit { get; set; }
    public float? FeatureSnapshotLocationMatch { get; set; }
    public float? FeatureSnapshotPropertyTypeMatch { get; set; }
    public float? FeatureSnapshotPriceToBudgetRatio { get; set; }
}
