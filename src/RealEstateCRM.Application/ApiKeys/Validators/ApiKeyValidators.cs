using FluentValidation;
using RealEstateCRM.Application.ApiKeys.DTOs;

namespace RealEstateCRM.Application.ApiKeys.Validators;

public class CreateApiKeyRequestValidator : AbstractValidator<CreateApiKeyRequest>
{
    private static readonly string[] ValidScopes = { "read", "read,write" };

    public CreateApiKeyRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Scopes).Must(s => ValidScopes.Contains(s)).WithMessage("Scopes must be 'read' or 'read,write'.");
    }
}
