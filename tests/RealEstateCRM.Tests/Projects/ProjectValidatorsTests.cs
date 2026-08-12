using RealEstateCRM.Application.Projects.DTOs;
using RealEstateCRM.Application.Projects.Validators;
using Xunit;

namespace RealEstateCRM.Tests.Projects;

public class ProjectValidatorsTests
{
    private readonly CreateProjectRequestValidator _validator = new();

    [Fact]
    public void Fails_WhenNameIsEmpty()
    {
        var result = _validator.Validate(new CreateProjectRequest { Name = "" });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Fails_WhenStartingPriceIsNegative()
    {
        var result = _validator.Validate(new CreateProjectRequest { Name = "Project", StartingPrice = -1 });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Succeeds_ForValidRequest()
    {
        var result = _validator.Validate(new CreateProjectRequest { Name = "Project", StartingPrice = 1_000_000 });

        Assert.True(result.IsValid);
    }
}
