using FluentValidation;
using RealEstateCRM.Application.Leads.DTOs;

namespace RealEstateCRM.Application.Leads.Validators;

public class CreateLeadRequestValidator : AbstractValidator<CreateLeadRequest>
{
    public CreateLeadRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Phone).MaximumLength(30);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.PreferredLocation).MaximumLength(200);
        RuleFor(x => x.PropertyType).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x.Source).IsInEnum();
        RuleFor(x => x.BudgetMin).GreaterThanOrEqualTo(0).When(x => x.BudgetMin.HasValue);
        RuleFor(x => x.BudgetMax).GreaterThanOrEqualTo(0).When(x => x.BudgetMax.HasValue);
        RuleFor(x => x)
            .Must(x => !x.BudgetMin.HasValue || !x.BudgetMax.HasValue || x.BudgetMin <= x.BudgetMax)
            .WithMessage("BudgetMin must be less than or equal to BudgetMax.")
            .WithName("BudgetMin");
    }
}

public class UpdateLeadRequestValidator : AbstractValidator<UpdateLeadRequest>
{
    public UpdateLeadRequestValidator()
    {
        Include(new CreateLeadRequestValidator());
        RuleFor(x => x.Status).IsInEnum();
    }
}

public class AssignLeadRequestValidator : AbstractValidator<AssignLeadRequest>
{
    public AssignLeadRequestValidator()
    {
        RuleFor(x => x.AgentId).NotEmpty();
    }
}

public class CreateLeadActivityRequestValidator : AbstractValidator<CreateLeadActivityRequest>
{
    public CreateLeadActivityRequestValidator()
    {
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}
