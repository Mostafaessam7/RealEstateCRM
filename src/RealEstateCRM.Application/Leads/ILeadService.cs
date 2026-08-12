using RealEstateCRM.Application.Common.Models;
using RealEstateCRM.Application.Leads.DTOs;

namespace RealEstateCRM.Application.Leads;

public interface ILeadService
{
    Task<PagedResult<LeadDto>> ListAsync(LeadListQuery query, CancellationToken cancellationToken = default);

    Task<LeadDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<LeadDto> CreateAsync(CreateLeadRequest request, CancellationToken cancellationToken = default);

    Task<LeadDto> UpdateAsync(Guid id, UpdateLeadRequest request, CancellationToken cancellationToken = default);

    /// <summary>Soft delete.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Assigns an unassigned lead to an agent. Fails if already assigned — use TransferAsync instead.</summary>
    Task<LeadDto> AssignAsync(Guid id, AssignLeadRequest request, CancellationToken cancellationToken = default);

    /// <summary>Reassigns an already-assigned lead to a different agent, logging the change.</summary>
    Task<LeadDto> TransferAsync(Guid id, AssignLeadRequest request, CancellationToken cancellationToken = default);
}
