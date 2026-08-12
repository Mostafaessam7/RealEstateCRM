using RealEstateCRM.Application.Commissions.DTOs;
using RealEstateCRM.Application.Commissions.Validators;
using Xunit;

namespace RealEstateCRM.Tests.Commissions;

public class CommissionValidatorsTests
{
    private readonly CreateCommissionRequestValidator _validator = new();

    [Fact]
    public void Fails_WhenCommissionPercentageIsZero()
    {
        var result = _validator.Validate(new CreateCommissionRequest { DealId = Guid.NewGuid(), CommissionPercentage = 0, CompanyCommissionPercentage = 2 });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Fails_WhenPercentageExceeds100()
    {
        var result = _validator.Validate(new CreateCommissionRequest { DealId = Guid.NewGuid(), CommissionPercentage = 150, CompanyCommissionPercentage = 2 });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Succeeds_ForValidRequest()
    {
        var result = _validator.Validate(new CreateCommissionRequest { DealId = Guid.NewGuid(), CommissionPercentage = 3, CompanyCommissionPercentage = 2 });

        Assert.True(result.IsValid);
    }
}
