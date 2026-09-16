using FluentValidation;

namespace AiEngineeringManagerCopilot.Application.Teams.Validation;

public sealed class UpdateTeamRequestValidator
    : AbstractValidator<UpdateTeamRequest>
{
    public UpdateTeamRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Team name is required.")
            .MaximumLength(200)
            .WithMessage("Team name cannot exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .WithMessage("Team description cannot exceed 2000 characters.")
            .When(x => x.Description is not null);
    }
}