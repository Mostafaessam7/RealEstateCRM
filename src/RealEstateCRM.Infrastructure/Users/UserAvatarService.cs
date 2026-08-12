using Microsoft.EntityFrameworkCore;
using RealEstateCRM.Application.Common.Exceptions;
using RealEstateCRM.Application.Common.Interfaces;
using RealEstateCRM.Application.Common.Storage;
using RealEstateCRM.Application.Users;
using RealEstateCRM.Infrastructure.Persistence;

namespace RealEstateCRM.Infrastructure.Users;

public class UserAvatarService : IUserAvatarService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantService _currentTenant;
    private readonly IBlobStorageService _blobStorage;

    public UserAvatarService(ApplicationDbContext db, ICurrentTenantService currentTenant, IBlobStorageService blobStorage)
    {
        _db = db;
        _currentTenant = currentTenant;
        _blobStorage = blobStorage;
    }

    public async Task<string> UploadAvatarAsync(
        Guid userId, Stream content, string fileName, string contentType, long sizeBytes,
        CancellationToken cancellationToken = default)
    {
        UploadValidation.EnsureImage(contentType, sizeBytes);

        // Users aren't ITenantEntity (no automatic tenant filter), so scope explicitly.
        var user = await _db.Users.FirstOrDefaultAsync(
            u => u.Id == userId && u.CompanyId == _currentTenant.CompanyId, cancellationToken)
            ?? throw new AppException("User not found.", 404);

        var companyId = _currentTenant.CompanyId!.Value;

        if (!string.IsNullOrEmpty(user.AvatarBlobPath))
        {
            await _blobStorage.DeleteAsync(user.AvatarBlobPath, cancellationToken);
        }

        var path = BlobPaths.UserAvatar(companyId, userId, fileName);
        var url = await _blobStorage.UploadAsync(path, content, contentType, cancellationToken);

        user.AvatarBlobPath = path;
        user.AvatarUrl = url;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return url;
    }
}
