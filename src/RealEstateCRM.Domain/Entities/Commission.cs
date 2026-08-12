using RealEstateCRM.Domain.Common;
using RealEstateCRM.Domain.Enums;

namespace RealEstateCRM.Domain.Entities;

/// <summary>No soft delete — financial record. Cancellation is a terminal status, not a delete.</summary>
public class Commission : TenantEntity
{
    public Guid DealId { get; set; }
    public Guid AgentId { get; set; }
    public decimal CommissionPercentage { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal CompanyCommission { get; set; }
    public CommissionStatus Status { get; set; } = CommissionStatus.Pending;
    public DateTime? PaymentDate { get; set; }
}
