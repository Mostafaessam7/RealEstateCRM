using FluentValidation;
using RealEstateCRM.Application.Deals.DTOs;

namespace RealEstateCRM.Application.Deals.Validators;

public class CreateDealRequestValidator : AbstractValidator<CreateDealRequest>
{
    public CreateDealRequestValidator()
    {
        RuleFor(x => x.LeadId).NotEmpty();
        RuleFor(x => x.UnitId).NotEmpty();
        RuleFor(x => x.DealValue).GreaterThan(0);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

public class UpdateDealRequestValidator : AbstractValidator<UpdateDealRequest>
{
    public UpdateDealRequestValidator()
    {
        RuleFor(x => x.DealValue).GreaterThan(0);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}
