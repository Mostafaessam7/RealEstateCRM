using RealEstateCRM.Application.Units.DTOs;

namespace RealEstateCRM.Application.Units;

public interface IUnitImageService
{
    Task<UnitImageDto> UploadAsync(
        Guid unitId, Stream content, string fileName, string contentType, long sizeBytes,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UnitImageDto>> ListAsync(Guid unitId, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid unitId, Guid imageId, CancellationToken cancellationToken = default);
}
