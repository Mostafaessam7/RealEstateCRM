using RealEstateCRM.Application.Units.DTOs;
using RealEstateCRM.Application.Units.Validators;
using Xunit;

namespace RealEstateCRM.Tests.Units;

public class UnitValidatorsTests
{
    private readonly CreateUnitRequestValidator _validator = new();

    [Fact]
    public void Fails_WhenUnitCodeIsEmpty()
    {
        var result = _validator.Validate(new CreateUnitRequest { ProjectId = Guid.NewGuid(), UnitCode = "", Price = 100 });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Fails_WhenPriceIsZeroOrNegative()
    {
        var result = _validator.Validate(new CreateUnitRequest { ProjectId = Guid.NewGuid(), UnitCode = "A-1", Price = 0 });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Succeeds_ForValidRequest()
    {
        var result = _validator.Validate(new CreateUnitRequest { ProjectId = Guid.NewGuid(), UnitCode = "A-1", Price = 1_000_000 });

        Assert.True(result.IsValid);
    }
}
