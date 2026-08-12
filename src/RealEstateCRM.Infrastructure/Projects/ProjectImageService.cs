using Microsoft.EntityFrameworkCore;
using RealEstateCRM.Application.Common.Exceptions;
using RealEstateCRM.Application.Common.Interfaces;
using RealEstateCRM.Application.Common.Storage;
using RealEstateCRM.Application.Projects;
using RealEstateCRM.Application.Projects.DTOs;
using RealEstateCRM.Domain.Entities;
using RealEstateCRM.Infrastructure.Persistence;

namespace RealEstateCRM.Infrastructure.Projects;

public class ProjectImageService : IProjectImageService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantService _currentTenant;
    private readonly IBlobStorageService _blobStorage;

    public ProjectImageService(ApplicationDbContext db, ICurrentTenantService currentTenant, IBlobStorageService blobStorage)
    {
        _db = db;
        _currentTenant = currentTenant;
        _blobStorage = blobStorage;
    }

    public async Task<ProjectImageDto> UploadAsync(
        Guid projectId, Stream content, string fileName, string contentType, long sizeBytes,
        CancellationToken cancellationToken = default)
    {
        UploadValidation.EnsureImage(contentType, sizeBytes);

        var projectExists = await _db.Projects.AnyAsync(p => p.Id == projectId, cancellationToken);
        if (!projectExists)
        {
            throw new AppException("Project not found.", 404);
        }

        var companyId = _currentTenant.CompanyId
            ?? throw new AppException("Authenticated company context is required.", 401);

        var imageId = Guid.NewGuid();
        var path = BlobPaths.ProjectImage(companyId, projectId, imageId, fileName);
        var url = await _blobStorage.UploadAsync(path, content, contentType, cancellationToken);

        var image = new ProjectImage
        {
            Id = imageId,
            ProjectId = projectId,
            BlobPath = path,
            Url = url,
            FileName = fileName,
            ContentType = contentType,
            SizeBytes = sizeBytes,
            CreatedAt = DateTime.UtcNow
        };

        _db.ProjectImages.Add(image);
        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(image);
    }

    public async Task<IReadOnlyList<ProjectImageDto>> ListAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var images = await _db.ProjectImages
            .AsNoTracking()
            .Where(i => i.ProjectId == projectId)
            .OrderBy(i => i.CreatedAt)
            .ToListAsync(cancellationToken);

        return images.Select(ToDto).ToList();
    }

    public async Task DeleteAsync(Guid projectId, Guid imageId, CancellationToken cancellationToken = default)
    {
        var image = await _db.ProjectImages.FirstOrDefaultAsync(
            i => i.Id == imageId && i.ProjectId == projectId, cancellationToken)
            ?? throw new AppException("Image not found.", 404);

        await _blobStorage.DeleteAsync(image.BlobPath, cancellationToken);

        _db.ProjectImages.Remove(image);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static ProjectImageDto ToDto(ProjectImage image) => new()
    {
        Id = image.Id,
        ProjectId = image.ProjectId,
        Url = image.Url,
        FileName = image.FileName,
        ContentType = image.ContentType,
        SizeBytes = image.SizeBytes,
        CreatedAt = image.CreatedAt
    };
}
