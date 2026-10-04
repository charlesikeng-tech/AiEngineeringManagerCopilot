using FluentValidation;

namespace AiEngineeringManagerCopilot.Application.Common;

public sealed record PageRequest(int PageNumber = 1, int PageSize = 10)
{
    public int Offset => checked((PageNumber - 1) * PageSize);
}

public static class PaginationValidation
{
    public static void AddPaginationRules<T>(
        this AbstractValidator<T> validator,
        System.Linq.Expressions.Expression<Func<T, int>> pageNumber,
        System.Linq.Expressions.Expression<Func<T, int>> pageSize)
    {
        validator.RuleFor(pageNumber).GreaterThanOrEqualTo(1);
        validator.RuleFor(pageSize).InclusiveBetween(1, 100);
        var number = pageNumber.Compile();
        var size = pageSize.Compile();
        validator.RuleFor(request => request)
            .Must(request => ((long)number(request) - 1) * size(request) <= int.MaxValue)
            .WithMessage("The requested page offset is too large.");
    }
}

public sealed class PageRequestValidator : AbstractValidator<PageRequest>
{
    public PageRequestValidator() =>
        this.AddPaginationRules(request => request.PageNumber, request => request.PageSize);
}
