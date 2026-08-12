using RealEstateCRM.Application.Leads.DTOs;
using RealEstateCRM.Application.Leads.Validators;
using RealEstateCRM.Domain.Enums;
using Xunit;

namespace RealEstateCRM.Tests.Leads;

public class LeadValidatorsTests
{
    private readonly CreateLeadRequestValidator _validator = new();

    [Fact]
    public void Fails_WhenFullNameIsEmpty()
    {
        var result = _validator.Validate(new CreateLeadRequest { FullName = "", Source = LeadSource.Website });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Fails_WhenBudgetMinGreaterThanBudgetMax()
    {
        var result = _validator.Validate(new CreateLeadRequest
        {
            FullName = "Lead",
            Source = LeadSource.Website,
            BudgetMin = 500_000,
            BudgetMax = 100_000
        });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Fails_WhenEmailIsInvalid()
    {
        var result = _validator.Validate(new CreateLeadRequest
        {
            FullName = "Lead",
            Source = LeadSource.Website,
            Email = "not-an-email"
        });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Succeeds_ForValidRequest()
    {
        var result = _validator.Validate(new CreateLeadRequest
        {
            FullName = "Lead",
            Source = LeadSource.Website,
            Email = "lead@example.com",
            BudgetMin = 100_000,
            BudgetMax = 500_000
        });

        Assert.True(result.IsValid);
    }
}
