using RealEstateCRM.Application.Common.Storage;
using Xunit;

namespace RealEstateCRM.Tests.Storage;

public class BlobPathsTests
{
    [Fact]
    public void ProjectImage_IsTenantScoped()
    {
        var companyId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var imageId = Guid.NewGuid();

        var path = BlobPaths.ProjectImage(companyId, projectId, imageId, "front.jpg");

        Assert.StartsWith($"companies/{companyId}/projects/{projectId}/", path);
        Assert.EndsWith("front.jpg", path);
    }

    [Fact]
    public void Document_SanitizesInvalidFileNameCharacters()
    {
        var path = BlobPaths.Document(Guid.NewGuid(), Guid.NewGuid(), "weird:name?.pdf");

        Assert.DoesNotContain(":", path.Split('/').Last());
        Assert.DoesNotContain("?", path.Split('/').Last());
    }
}
