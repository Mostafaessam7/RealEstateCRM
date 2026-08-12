namespace RealEstateCRM.Application.Common.Storage;

/// <summary>
/// Tenant-scoped blob path formats from docs/multi-tenancy.md#blob-storage. Centralized so
/// every uploader builds paths identically. Paths are not an authorization mechanism —
/// service methods must still check the caller is allowed to see the entity first.
/// </summary>
public static class BlobPaths
{
    public static string ProjectImage(Guid companyId, Guid projectId, Guid imageId, string fileName) =>
        $"companies/{companyId}/projects/{projectId}/{imageId}-{Sanitize(fileName)}";

    public static string UnitImage(Guid companyId, Guid unitId, Guid imageId, string fileName) =>
        $"companies/{companyId}/units/{unitId}/{imageId}-{Sanitize(fileName)}";

    public static string UserAvatar(Guid companyId, Guid userId, string fileName) =>
        $"companies/{companyId}/users/{userId}/avatar-{Sanitize(fileName)}";

    public static string Document(Guid companyId, Guid documentId, string fileName) =>
        $"companies/{companyId}/documents/{documentId}/{Sanitize(fileName)}";

    private static string Sanitize(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(fileName.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
    }
}
