using FluentValidation;
using RealEstateCRM.Application.Commissions.DTOs;

namespace RealEstateCRM.Application.Commissions.Validators;

public class CreateCommissionRequestValidator : AbstractValidator<CreateCommissionRequest>
{
    public CreateCommissionRequestValidator()
    {
        RuleFor(x => x.DealId).NotEmpty();
        RuleFor(x => x.CommissionPercentage).GreaterThan(0).LessThanOrEqualTo(100);
        RuleFor(x => x.CompanyCommissionPercentage).GreaterThanOrEqualTo(0).LessThanOrEqualTo(100);
    }
}
