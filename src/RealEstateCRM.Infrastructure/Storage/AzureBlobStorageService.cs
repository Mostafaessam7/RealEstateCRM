using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using RealEstateCRM.Application.Common.Interfaces;

namespace RealEstateCRM.Infrastructure.Storage;

public class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient _container;

    public AzureBlobStorageService(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("AzureBlobStorage");
        var containerName = configuration["BlobStorage:ContainerName"] ?? "media";
        _container = new BlobContainerClient(connectionString, containerName);
    }

    public async Task<string> UploadAsync(string path, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        await _container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);

        var blob = _container.GetBlobClient(path);
        await blob.UploadAsync(
            content,
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } },
            cancellationToken);

        return blob.Uri.ToString();
    }

    public Task DeleteAsync(string path, CancellationToken cancellationToken = default) =>
        _container.GetBlobClient(path).DeleteIfExistsAsync(cancellationToken: cancellationToken);

    public string GetUrl(string path) => _container.GetBlobClient(path).Uri.ToString();
}
