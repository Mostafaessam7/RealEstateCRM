using FluentValidation;
using RealEstateCRM.Application.WhatsApp.DTOs;

namespace RealEstateCRM.Application.WhatsApp.Validators;

public class CreateWhatsAppTemplateRequestValidator : AbstractValidator<CreateWhatsAppTemplateRequest>
{
    public CreateWhatsAppTemplateRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(2000);
    }
}

public class UpdateWhatsAppTemplateRequestValidator : AbstractValidator<UpdateWhatsAppTemplateRequest>
{
    public UpdateWhatsAppTemplateRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(2000);
    }
}

public class SendWhatsAppRequestValidator : AbstractValidator<SendWhatsAppRequest>
{
    public SendWhatsAppRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => x.TemplateId.HasValue || !string.IsNullOrWhiteSpace(x.Body))
            .WithMessage("Either a template or a message body is required.");

        RuleFor(x => x.Body).MaximumLength(2000);
    }
}
