using FluentValidation;

namespace AiEngineeringManagerCopilot.Application.TeamMembers.Validation;

public sealed class UpdateTeamMemberRequestValidator
    : AbstractValidator<UpdateTeamMemberRequest>
{
    public UpdateTeamMemberRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Member name is required.")
            .MaximumLength(200)
            .WithMessage("Member name cannot exceed 200 characters.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Member email is required.")
            .EmailAddress()
            .WithMessage("Member email must be a valid email address.")
            .MaximumLength(320)
            .WithMessage("Member email cannot exceed 320 characters.");

        RuleFor(x => x.ProviderUserId)
            .MaximumLength(200)
            .WithMessage("Provider user ID cannot exceed 200 characters.")
            .When(x => x.ProviderUserId is not null);
    }
}