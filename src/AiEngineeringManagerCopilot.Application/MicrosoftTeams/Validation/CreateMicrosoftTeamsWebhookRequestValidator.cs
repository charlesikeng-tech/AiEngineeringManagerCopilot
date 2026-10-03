using FluentValidation;

namespace AiEngineeringManagerCopilot.Application.MicrosoftTeams.Validation;

public sealed class CreateMicrosoftTeamsWebhookRequestValidator
    : AbstractValidator<CreateMicrosoftTeamsWebhookRequest>
{
    public CreateMicrosoftTeamsWebhookRequestValidator()
    {
        RuleFor(request => request.WebhookUrl)
            .NotEmpty()
            .MaximumLength(4096)
            .Must(value => MicrosoftTeamsWebhookAddress.TryParse(value, out _))
            .WithMessage("Enter a valid Microsoft Teams webhook URL.");
    }
}
