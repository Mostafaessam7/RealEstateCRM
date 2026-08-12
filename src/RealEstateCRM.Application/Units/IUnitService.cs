using RealEstateCRM.Application.Common.Models;
using RealEstateCRM.Application.Units.DTOs;

namespace RealEstateCRM.Application.Units;

public interface IUnitService
{
    Task<PagedResult<UnitDto>> ListAsync(UnitListQuery query, CancellationToken cancellationToken = default);

    Task<UnitDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<UnitDto> CreateAsync(CreateUnitRequest request, CancellationToken cancellationToken = default);

    Task<UnitDto> UpdateAsync(Guid id, UpdateUnitRequest request, CancellationToken cancellationToken = default);

    /// <summary>Soft delete.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Units currently Status == Available, optionally scoped to one project.</summary>
    Task<IReadOnlyList<UnitDto>> GetAvailableAsync(Guid? projectId, CancellationToken cancellationToken = default);
}
