using RealEstateCRM.Application.Projects.DTOs;

namespace RealEstateCRM.Application.Projects;

public interface IProjectImageService
{
    Task<ProjectImageDto> UploadAsync(
        Guid projectId, Stream content, string fileName, string contentType, long sizeBytes,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProjectImageDto>> ListAsync(Guid projectId, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid projectId, Guid imageId, CancellationToken cancellationToken = default);
}
