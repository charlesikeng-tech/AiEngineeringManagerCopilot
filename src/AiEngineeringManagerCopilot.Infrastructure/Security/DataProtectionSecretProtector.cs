using AiEngineeringManagerCopilot.Application.Abstractions;
using Microsoft.AspNetCore.DataProtection;

namespace AiEngineeringManagerCopilot.Infrastructure.Security;

public sealed class DataProtectionSecretProtector(
    IDataProtectionProvider dataProtectionProvider)
    : ISecretProtector
{
    private const string Purpose = "AiEngineeringManagerCopilot.GitHub.AccessToken";

    private readonly IDataProtector _protector =
        dataProtectionProvider.CreateProtector(Purpose);

    public string Protect(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return _protector.Protect(value);
    }

    public string Unprotect(string protectedValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(protectedValue);

        return _protector.Unprotect(protectedValue);
    }
}