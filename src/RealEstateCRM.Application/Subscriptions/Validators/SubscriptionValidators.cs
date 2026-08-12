using FluentValidation;
using RealEstateCRM.Application.Subscriptions.DTOs;

namespace RealEstateCRM.Application.Subscriptions.Validators;

public class CreateSubscriptionPlanRequestValidator : AbstractValidator<CreateSubscriptionPlanRequest>
{
    public CreateSubscriptionPlanRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.MonthlyPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxUsers).GreaterThan(0);
        RuleFor(x => x.MaxLeads).GreaterThan(0);
        RuleFor(x => x.MaxUnits).GreaterThan(0);
    }
}

public class UpdateSubscriptionPlanRequestValidator : AbstractValidator<UpdateSubscriptionPlanRequest>
{
    public UpdateSubscriptionPlanRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.MonthlyPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxUsers).GreaterThan(0);
        RuleFor(x => x.MaxLeads).GreaterThan(0);
        RuleFor(x => x.MaxUnits).GreaterThan(0);
    }
}

public class ChangePlanRequestValidator : AbstractValidator<ChangePlanRequest>
{
    public ChangePlanRequestValidator()
    {
        RuleFor(x => x.PlanCode).NotEmpty().MaximumLength(30);
    }
}
