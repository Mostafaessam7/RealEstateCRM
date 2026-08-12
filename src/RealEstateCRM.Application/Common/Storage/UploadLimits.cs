namespace RealEstateCRM.Application.Common.Storage;

public static class UploadLimits
{
    public static readonly HashSet<string> ImageContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp"
    };

    public const long MaxImageSizeBytes = 5 * 1024 * 1024; // 5 MB
    public const long MaxDocumentSizeBytes = 20 * 1024 * 1024; // 20 MB
}
