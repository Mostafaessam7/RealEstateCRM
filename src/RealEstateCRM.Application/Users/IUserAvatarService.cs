namespace RealEstateCRM.Application.Users;

public interface IUserAvatarService
{
    /// <returns>The uploaded avatar's URL.</returns>
    Task<string> UploadAvatarAsync(
        Guid userId, Stream content, string fileName, string contentType, long sizeBytes,
        CancellationToken cancellationToken = default);
}
