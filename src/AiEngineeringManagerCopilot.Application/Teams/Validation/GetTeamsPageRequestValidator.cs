using FluentValidation;
using AiEngineeringManagerCopilot.Application.Common;

namespace AiEngineeringManagerCopilot.Application.Teams.Validation;

public sealed class GetTeamsPageRequestValidator : AbstractValidator<GetTeamsPageRequest>
{
    public GetTeamsPageRequestValidator()
    {
        this.AddPaginationRules(request => request.PageNumber, request => request.PageSize);
        RuleFor(request => request.Search).MaximumLength(200);
    }
}
