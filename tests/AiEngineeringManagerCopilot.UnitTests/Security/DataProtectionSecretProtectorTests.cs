using AiEngineeringManagerCopilot.Infrastructure.Security;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.DataProtection;
using Xunit;

namespace AiEngineeringManagerCopilot.UnitTests.Security;

public sealed class DataProtectionSecretProtectorTests
{
    private static DataProtectionSecretProtector CreateProtector()
    {
        var services = new ServiceCollection();

        services.AddDataProtection();

        using var serviceProvider = services.BuildServiceProvider();

        var dataProtectionProvider =
            serviceProvider.GetRequiredService<IDataProtectionProvider>();

        return new DataProtectionSecretProtector(
            dataProtectionProvider);
    }

    [Fact]
    public void Protect_ShouldNotReturnOriginalValue()
    {
        var protector = CreateProtector();

        const string token = "github-secret-token-123";

        var protectedValue = protector.Protect(token);

        protectedValue.Should().NotBe(token);
    }

    [Fact]
    public void Protect_ThenUnprotect_ShouldReturnOriginalValue()
    {
        var protector = CreateProtector();

        const string token = "github-secret-token-123";

        var protectedValue = protector.Protect(token);
        var unprotectedValue = protector.Unprotect(protectedValue);

        unprotectedValue.Should().Be(token);
    }

    [Fact]
    public void Protect_SameValueTwice_ShouldProduceDifferentValues()
    {
        var protector = CreateProtector();

        const string token = "github-secret-token-123";

        var first = protector.Protect(token);
        var second = protector.Protect(token);

        first.Should().NotBe(second);
    }
}