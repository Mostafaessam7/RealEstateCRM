using RealEstateCRM.Application.Commissions.DTOs;
using RealEstateCRM.Application.Common.Models;

namespace RealEstateCRM.Application.Commissions;

public interface ICommissionService
{
    Task<PagedResult<CommissionDto>> ListAsync(CommissionListQuery query, CancellationToken cancellationToken = default);

    Task<CommissionDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Calculates and creates a commission for a Contracted deal. One per deal.</summary>
    Task<CommissionDto> CreateAsync(CreateCommissionRequest request, CancellationToken cancellationToken = default);

    /// <summary>Pending -> Paid. Sets PaymentDate.</summary>
    Task<CommissionDto> MarkPaidAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Pending -> Cancelled.</summary>
    Task<CommissionDto> CancelAsync(Guid id, CancellationToken cancellationToken = default);
}
