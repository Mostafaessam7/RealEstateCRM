using RealEstateCRM.Application.Deals.DTOs;
using RealEstateCRM.Application.Deals.Validators;
using Xunit;

namespace RealEstateCRM.Tests.Deals;

public class DealValidatorsTests
{
    private readonly CreateDealRequestValidator _validator = new();

    [Fact]
    public void Fails_WhenDealValueIsZeroOrNegative()
    {
        var result = _validator.Validate(new CreateDealRequest { LeadId = Guid.NewGuid(), UnitId = Guid.NewGuid(), DealValue = 0 });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Fails_WhenLeadOrUnitIdMissing()
    {
        var result = _validator.Validate(new CreateDealRequest { LeadId = Guid.Empty, UnitId = Guid.Empty, DealValue = 100 });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Succeeds_ForValidRequest()
    {
        var result = _validator.Validate(new CreateDealRequest { LeadId = Guid.NewGuid(), UnitId = Guid.NewGuid(), DealValue = 1_000_000 });

        Assert.True(result.IsValid);
    }
}
