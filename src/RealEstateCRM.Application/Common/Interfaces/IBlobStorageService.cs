namespace RealEstateCRM.Application.Common.Interfaces;

/// <summary>
/// Callers must build tenant-scoped paths themselves — see BlobPaths — and pass them in.
/// Blob paths are not an authorization mechanism; the service layer must still authorize
/// access before ever handing back a URL. See docs/multi-tenancy.md#blob-storage.
/// </summary>
public interface IBlobStorageService
{
    /// <returns>The blob's URL.</returns>
    Task<string> UploadAsync(string path, Stream content, string contentType, CancellationToken cancellationToken = default);

    Task DeleteAsync(string path, CancellationToken cancellationToken = default);

    string GetUrl(string path);
}
