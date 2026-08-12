using FluentValidation;
using RealEstateCRM.Application.Users.DTOs;
using RealEstateCRM.Domain.Constants;

namespace RealEstateCRM.Application.Users.Validators;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.Role).NotEmpty().Must(Roles.All.Contains).WithMessage("Role must be one of: " + string.Join(", ", Roles.All));
    }
}

public class UpdateUserRoleRequestValidator : AbstractValidator<UpdateUserRoleRequest>
{
    public UpdateUserRoleRequestValidator()
    {
        RuleFor(x => x.Role).NotEmpty().Must(Roles.All.Contains).WithMessage("Role must be one of: " + string.Join(", ", Roles.All));
    }
}
