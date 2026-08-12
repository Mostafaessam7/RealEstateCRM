using FluentValidation;
using RealEstateCRM.Application.Tasks.DTOs;

namespace RealEstateCRM.Application.Tasks.Validators;

public class CreateTaskItemRequestValidator : AbstractValidator<CreateTaskItemRequest>
{
    public CreateTaskItemRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.AssignedToUserId).NotEmpty();
        RuleFor(x => x.Priority).IsInEnum();
    }
}

public class UpdateTaskItemRequestValidator : AbstractValidator<UpdateTaskItemRequest>
{
    public UpdateTaskItemRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Priority).IsInEnum();
    }
}

public class AssignTaskItemRequestValidator : AbstractValidator<AssignTaskItemRequest>
{
    public AssignTaskItemRequestValidator()
    {
        RuleFor(x => x.AssignedToUserId).NotEmpty();
    }
}
