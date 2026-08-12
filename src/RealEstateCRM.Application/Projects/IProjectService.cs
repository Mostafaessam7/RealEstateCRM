using RealEstateCRM.Application.Common.Models;
using RealEstateCRM.Application.Projects.DTOs;

namespace RealEstateCRM.Application.Projects;

public interface IProjectService
{
    Task<PagedResult<ProjectDto>> ListAsync(ProjectListQuery query, CancellationToken cancellationToken = default);

    Task<ProjectDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ProjectDto> CreateAsync(CreateProjectRequest request, CancellationToken cancellationToken = default);

    Task<ProjectDto> UpdateAsync(Guid id, UpdateProjectRequest request, CancellationToken cancellationToken = default);

    /// <summary>Soft delete.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
