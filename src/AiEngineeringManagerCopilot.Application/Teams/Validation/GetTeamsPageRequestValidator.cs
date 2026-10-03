using FluentValidation;

namespace AiEngineeringManagerCopilot.Application.Teams.Validation;

public sealed class GetTeamsPageRequestValidator : AbstractValidator<GetTeamsPageRequest>
{
    public GetTeamsPageRequestValidator()
    {
        RuleFor(request => request.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(request => request.PageSize).InclusiveBetween(1, 100);
        RuleFor(request => request.Search).MaximumLength(200);
        RuleFor(request => request)
            .Must(request => ((long)request.PageNumber - 1) * request.PageSize <= int.MaxValue)
            .WithMessage("The requested page offset is too large.");
    }
}
