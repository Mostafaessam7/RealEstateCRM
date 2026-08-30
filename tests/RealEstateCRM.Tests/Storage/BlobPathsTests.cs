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

    [Theory]
    [InlineData('"')]
    [InlineData('<')]
    [InlineData('>')]
    [InlineData('|')]
    [InlineData(':')]
    [InlineData('*')]
    [InlineData('?')]
    public void Document_SanitizesEveryCharacterWindowsRejects(char invalid)
    {
        // The implementation used Path.GetInvalidFileNameChars(), which is platform-dependent: on
        // Linux it returns only '/' and NUL, so none of these were being replaced when the API ran
        // in a container. The set is now explicit, and this pins it so nobody "simplifies" it back
        // to the framework call — which looks more correct and silently is not.
        var path = BlobPaths.Document(Guid.NewGuid(), Guid.NewGuid(), $"file{invalid}name.pdf");

        Assert.DoesNotContain(invalid, path.Split('/').Last());
    }

    [Fact]
    public void Document_KeepsATraversingFileNameInsideItsOwnFolder()
    {
        // A file name is attacker-supplied. Separators have to be neutralised or the blob lands
        // outside its tenant's prefix entirely, which turns a naming detail into cross-tenant
        // access. Asserted on the segment count rather than the string, so it fails if the name
        // introduces any new path level at all.
        var companyId = Guid.NewGuid();
        var documentId = Guid.NewGuid();

        var path = BlobPaths.Document(companyId, documentId, "../../other-company/secret.pdf");

        Assert.StartsWith($"companies/{companyId}/documents/{documentId}/", path);

        // Five segments: companies / {companyId} / documents / {documentId} / <file name>. The
        // name contributing a sixth would mean it introduced a level of its own.
        Assert.Equal(5, path.Split('/').Length);

        // Asserting on separators rather than on "..": once '/' and '\' are replaced the remaining
        // dots are ordinary characters in a file name and traverse nothing. Requiring ".." to be
        // gone as well would be testing a stricter rule than the one that matters, and would fail
        // for a legitimate name like "report..final.pdf".
        var fileSegment = path.Split('/').Last();
        Assert.DoesNotContain('/', fileSegment);
        Assert.DoesNotContain('\\', fileSegment);
    }
}
