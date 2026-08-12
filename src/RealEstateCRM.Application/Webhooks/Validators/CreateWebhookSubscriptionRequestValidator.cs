using FluentValidation;
using RealEstateCRM.Application.Webhooks.DTOs;

namespace RealEstateCRM.Application.Webhooks.Validators;

public class CreateWebhookSubscriptionRequestValidator : AbstractValidator<CreateWebhookSubscriptionRequest>
{
    public CreateWebhookSubscriptionRequestValidator()
    {
        RuleFor(x => x.Url).NotEmpty().MaximumLength(500)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == "https" || uri.Scheme == "http"))
            .WithMessage("Url must be a valid absolute http(s) URL.");

        RuleFor(x => x.EventTypes).NotEmpty().WithMessage("At least one event type is required.");
        RuleForEach(x => x.EventTypes).Must(WebhookEventTypes.All.Contains)
            .WithMessage($"Event type must be one of: {string.Join(", ", WebhookEventTypes.All)}.");
    }
}
