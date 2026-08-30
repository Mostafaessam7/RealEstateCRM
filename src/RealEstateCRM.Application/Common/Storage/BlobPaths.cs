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

    /// <summary>
    /// Characters replaced in an uploaded file name before it becomes part of a blob path.
    /// </summary>
    /// <remarks>
    /// Deliberately an explicit set rather than <see cref="Path.GetInvalidFileNameChars"/>, which
    /// is <b>platform-dependent</b>: on Windows it includes <c>:</c>, <c>?</c>, <c>*</c> and the
    /// rest, while on Linux it returns only <c>/</c> and NUL. Using it meant the same upload was
    /// sanitised differently depending on the host the API happened to run on — fine on a
    /// developer's Windows machine, and not sanitised at all on a Linux container in production.
    ///
    /// This is the set Windows rejects, applied everywhere, because the file name can travel: a
    /// blob uploaded from Linux may later be downloaded by a Windows client that cannot save
    /// "weird:name?.pdf" at all. Sanitising to the stricter platform is the portable choice.
    /// </remarks>
    private static readonly char[] InvalidFileNameChars =
        ['"', '<', '>', '|', '\0', ':', '*', '?', '\\', '/'];

    private static string Sanitize(string fileName)
    {
        var sanitized = fileName
            .Select(c => InvalidFileNameChars.Contains(c) || char.IsControl(c) ? '_' : c)
            .ToArray();

        return new string(sanitized);
    }
}
