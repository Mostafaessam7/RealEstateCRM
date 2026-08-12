using Microsoft.EntityFrameworkCore;
using RealEstateCRM.Application.Common.Exceptions;
using RealEstateCRM.Application.Common.Interfaces;
using RealEstateCRM.Application.Common.Storage;
using RealEstateCRM.Application.Units;
using RealEstateCRM.Application.Units.DTOs;
using RealEstateCRM.Domain.Entities;
using RealEstateCRM.Infrastructure.Persistence;

namespace RealEstateCRM.Infrastructure.Units;

public class UnitImageService : IUnitImageService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantService _currentTenant;
    private readonly IBlobStorageService _blobStorage;

    public UnitImageService(ApplicationDbContext db, ICurrentTenantService currentTenant, IBlobStorageService blobStorage)
    {
        _db = db;
        _currentTenant = currentTenant;
        _blobStorage = blobStorage;
    }

    public async Task<UnitImageDto> UploadAsync(
        Guid unitId, Stream content, string fileName, string contentType, long sizeBytes,
        CancellationToken cancellationToken = default)
    {
        UploadValidation.EnsureImage(contentType, sizeBytes);

        var unitExists = await _db.Units.AnyAsync(u => u.Id == unitId, cancellationToken);
        if (!unitExists)
        {
            throw new AppException("Unit not found.", 404);
        }

        var companyId = _currentTenant.CompanyId
            ?? throw new AppException("Authenticated company context is required.", 401);

        var imageId = Guid.NewGuid();
        var path = BlobPaths.UnitImage(companyId, unitId, imageId, fileName);
        var url = await _blobStorage.UploadAsync(path, content, contentType, cancellationToken);

        var image = new UnitImage
        {
            Id = imageId,
            UnitId = unitId,
            BlobPath = path,
            Url = url,
            FileName = fileName,
            ContentType = contentType,
            SizeBytes = sizeBytes,
            CreatedAt = DateTime.UtcNow
        };

        _db.UnitImages.Add(image);
        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(image);
    }

    public async Task<IReadOnlyList<UnitImageDto>> ListAsync(Guid unitId, CancellationToken cancellationToken = default)
    {
        var images = await _db.UnitImages
            .AsNoTracking()
            .Where(i => i.UnitId == unitId)
            .OrderBy(i => i.CreatedAt)
            .ToListAsync(cancellationToken);

        return images.Select(ToDto).ToList();
    }

    public async Task DeleteAsync(Guid unitId, Guid imageId, CancellationToken cancellationToken = default)
    {
        var image = await _db.UnitImages.FirstOrDefaultAsync(
            i => i.Id == imageId && i.UnitId == unitId, cancellationToken)
            ?? throw new AppException("Image not found.", 404);

        await _blobStorage.DeleteAsync(image.BlobPath, cancellationToken);

        _db.UnitImages.Remove(image);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static UnitImageDto ToDto(UnitImage image) => new()
    {
        Id = image.Id,
        UnitId = image.UnitId,
        Url = image.Url,
        FileName = image.FileName,
        ContentType = image.ContentType,
        SizeBytes = image.SizeBytes,
        CreatedAt = image.CreatedAt
    };
}
