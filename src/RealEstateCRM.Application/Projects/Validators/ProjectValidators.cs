using FluentValidation;
using RealEstateCRM.Application.Projects.DTOs;

namespace RealEstateCRM.Application.Projects.Validators;

public class CreateProjectRequestValidator : AbstractValidator<CreateProjectRequest>
{
    public CreateProjectRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Developer).MaximumLength(200);
        RuleFor(x => x.Location).MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.StartingPrice).GreaterThanOrEqualTo(0).When(x => x.StartingPrice.HasValue);
        RuleFor(x => x.Status).IsInEnum();
    }
}

public class UpdateProjectRequestValidator : AbstractValidator<UpdateProjectRequest>
{
    public UpdateProjectRequestValidator()
    {
        Include(new CreateProjectRequestValidator());
    }
}
