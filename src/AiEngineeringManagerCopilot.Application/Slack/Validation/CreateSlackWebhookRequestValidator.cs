using FluentValidation;

namespace AiEngineeringManagerCopilot.Application.Slack.Validation;

public sealed class CreateSlackWebhookRequestValidator
    : AbstractValidator<CreateSlackWebhookRequest>
{
    public CreateSlackWebhookRequestValidator()
    {
        RuleFor(request => request.WebhookUrl)
            .NotEmpty()
            .MaximumLength(2048)
            .Must(value => SlackWebhookAddress.TryParse(value, out _))
            .WithMessage("Enter a valid Slack Incoming Webhook URL.");
    }
}
