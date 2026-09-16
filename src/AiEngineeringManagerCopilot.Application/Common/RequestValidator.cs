using FluentValidation;

namespace AiEngineeringManagerCopilot.Application.Common;

public static class RequestValidator
{
    public static async Task ValidateAndThrowAsync<T>(
        T request,
        IValidator<T> validator,
        CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(
            request,
            cancellationToken);

        if (result.IsValid)
        {
            return;
        }

        var errors = result.Errors
            .GroupBy(x => x.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(x => x.ErrorMessage)
                    .Distinct()
                    .ToArray());

        throw new ValidationException(errors);
    }
}