using RealEstateCRM.Application.Common.Exceptions;

namespace RealEstateCRM.Application.Common.Storage;

public static class UploadValidation
{
    public static void EnsureImage(string contentType, long sizeBytes)
    {
        if (!UploadLimits.ImageContentTypes.Contains(contentType))
        {
            throw new AppException("Only JPEG, PNG, or WEBP images are allowed.", 400);
        }

        if (sizeBytes <= 0 || sizeBytes > UploadLimits.MaxImageSizeBytes)
        {
            throw new AppException($"Image must be between 1 byte and {UploadLimits.MaxImageSizeBytes / 1024 / 1024} MB.", 400);
        }
    }

    public static void EnsureDocument(long sizeBytes)
    {
        if (sizeBytes <= 0 || sizeBytes > UploadLimits.MaxDocumentSizeBytes)
        {
            throw new AppException($"File must be between 1 byte and {UploadLimits.MaxDocumentSizeBytes / 1024 / 1024} MB.", 400);
        }
    }
}
