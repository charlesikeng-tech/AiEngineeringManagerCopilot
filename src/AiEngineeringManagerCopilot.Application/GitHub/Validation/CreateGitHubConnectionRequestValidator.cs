using FluentValidation;

namespace AiEngineeringManagerCopilot.Application.GitHub.Validation;

public sealed class CreateGitHubConnectionRequestValidator
    : AbstractValidator<CreateGitHubConnectionRequest>
{
    public CreateGitHubConnectionRequestValidator()
    {
        RuleFor(x => x.Owner)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.OwnerType)
            .IsInEnum();

        RuleFor(x => x.AccessToken)
            .NotEmpty()
            .MaximumLength(1000);
    }
}