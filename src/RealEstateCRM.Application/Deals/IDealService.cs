using RealEstateCRM.Application.Common.Models;
using RealEstateCRM.Application.Deals.DTOs;

namespace RealEstateCRM.Application.Deals;

public interface IDealService
{
    Task<PagedResult<DealDto>> ListAsync(DealListQuery query, CancellationToken cancellationToken = default);

    Task<DealDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Creates a deal in DealStatus.Pending. The unit must currently be Available.</summary>
    Task<DealDto> CreateAsync(CreateDealRequest request, CancellationToken cancellationToken = default);

    /// <summary>DealValue/Notes only — not status.</summary>
    Task<DealDto> UpdateAsync(Guid id, UpdateDealRequest request, CancellationToken cancellationToken = default);

    /// <summary>Pending -> Reserved. Marks the unit Reserved.</summary>
    Task<DealDto> ReserveAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Reserved -> Contracted. Marks the unit Sold.</summary>
    Task<DealDto> ContractAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Pending/Reserved -> Cancelled. Reverts the unit to Available if it was Reserved for this deal.</summary>
    Task<DealDto> CancelAsync(Guid id, CancellationToken cancellationToken = default);
}
