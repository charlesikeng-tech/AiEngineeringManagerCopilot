using Microsoft.Extensions.Options;

namespace AiEngineeringManagerCopilot.Api.Authentication;

public sealed class JwtOptionsValidator
    : IValidateOptions<JwtOptions>
{
    private const int MinimumKeyLength = 32;

    public ValidateOptionsResult Validate(
        string? name,
        JwtOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            failures.Add("Jwt:Issuer is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            failures.Add("Jwt:Audience is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Key))
        {
            failures.Add("Jwt:Key is required.");
        }
        else if (options.Key.Length < MinimumKeyLength)
        {
            failures.Add(
                $"Jwt:Key must be at least {MinimumKeyLength} characters.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}