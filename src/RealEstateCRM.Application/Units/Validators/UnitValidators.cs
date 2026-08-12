using FluentValidation;
using RealEstateCRM.Application.Units.DTOs;

namespace RealEstateCRM.Application.Units.Validators;

public class CreateUnitRequestValidator : AbstractValidator<CreateUnitRequest>
{
    public CreateUnitRequestValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.UnitCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.PropertyType).MaximumLength(100);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.Area).GreaterThan(0).When(x => x.Area.HasValue);
        RuleFor(x => x.Bedrooms).GreaterThanOrEqualTo(0).When(x => x.Bedrooms.HasValue);
        RuleFor(x => x.Bathrooms).GreaterThanOrEqualTo(0).When(x => x.Bathrooms.HasValue);
        RuleFor(x => x.Floor).MaximumLength(30);
        RuleFor(x => x.Location).MaximumLength(200);
        RuleFor(x => x.DownPayment).GreaterThanOrEqualTo(0).When(x => x.DownPayment.HasValue);
        RuleFor(x => x.InstallmentYears).GreaterThanOrEqualTo(0).When(x => x.InstallmentYears.HasValue);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Status).IsInEnum();
    }
}

public class UpdateUnitRequestValidator : AbstractValidator<UpdateUnitRequest>
{
    public UpdateUnitRequestValidator()
    {
        Include(new CreateUnitRequestValidator());
    }
}
