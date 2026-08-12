using FluentValidation;
using RealEstateCRM.Application.Marketing.DTOs;
using RealEstateCRM.Domain.Enums;

namespace RealEstateCRM.Application.Marketing.Validators;

public class CreateCampaignRequestValidator : AbstractValidator<CreateCampaignRequest>
{
    public CreateCampaignRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.Subject).MaximumLength(200);
        RuleFor(x => x.Subject).NotEmpty().When(x => x.Channel == CampaignChannel.Email)
            .WithMessage("Subject is required for an Email campaign.");
    }
}
