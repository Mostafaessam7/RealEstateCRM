using RealEstateCRM.Application.Common.Interfaces;

namespace RealEstateCRM.Tests.MultiTenancy;

/// <summary>Records uploads/deletes without touching real Azure Storage or an emulator.</summary>
internal class InMemoryBlobStorageService : IBlobStorageService
{
    public List<string> UploadedPaths { get; } = new();
    public List<string> DeletedPaths { get; } = new();

    public Task<string> UploadAsync(string path, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        UploadedPaths.Add(path);
        return Task.FromResult(GetUrl(path));
    }

    public Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        DeletedPaths.Add(path);
        return Task.CompletedTask;
    }

    public string GetUrl(string path) => $"https://fake.blob.local/media/{path}";
}
