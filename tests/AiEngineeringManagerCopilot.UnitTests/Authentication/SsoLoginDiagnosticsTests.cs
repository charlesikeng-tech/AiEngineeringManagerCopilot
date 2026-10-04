using System.Net;
using System.Security.Cryptography;
using AiEngineeringManagerCopilot.Api.Authentication.Sso;
using Microsoft.Extensions.Logging;

namespace AiEngineeringManagerCopilot.UnitTests.Authentication;

public sealed class SsoLoginDiagnosticsTests
{
    [Fact]
    public void ReportFailure_logs_only_safe_fields_and_returns_a_unique_reference()
    {
        var logger = new CaptureLogger();
        var diagnostics = new SsoLoginDiagnostics(logger) { Stage = SsoLoginStage.AuthorizationChallenge };
        var provider = Guid.NewGuid();
        var exception = new InvalidOperationException("secret-client-value",
            new HttpRequestException("sensitive-provider-response", null, HttpStatusCode.Forbidden));

        var reference = diagnostics.ReportFailure(exception, provider, 3);
        Assert.True(Guid.TryParseExact(reference, "N", out _));
        Assert.Contains(reference, logger.Message);
        Assert.Contains(provider.ToString(), logger.Message);
        Assert.Contains("AuthorizationChallenge", logger.Message);
        Assert.Contains("InvalidOperationException", logger.Message);
        Assert.Contains("HttpRequestException", logger.Message);
        Assert.Contains("403", logger.Message);
        Assert.DoesNotContain("secret-client-value", logger.Message);
        Assert.DoesNotContain("sensitive-provider-response", logger.Message);
        Assert.Null(logger.Exception);
        Assert.NotEqual(reference, diagnostics.ReportFailure(exception, provider, 3));
    }

    [Fact]
    public void Secret_decryption_failure_never_logs_ciphertext_or_exception_message()
    {
        var logger = new CaptureLogger();
        var diagnostics = new SsoLoginDiagnostics(logger) { Stage = SsoLoginStage.SecretDecryption };
        diagnostics.ReportFailure(new CryptographicException("protected-secret-and-key-details"), Guid.NewGuid(), 1);
        Assert.Contains("SecretDecryption", logger.Message);
        Assert.Contains("CryptographicException", logger.Message);
        Assert.DoesNotContain("protected-secret-and-key-details", logger.Message);
        Assert.Null(logger.Exception);
    }

    [Fact]
    public void Protocol_diagnostics_extract_only_known_error_hints_and_fixed_format_library_codes()
    {
        var logger = new CaptureLogger();
        var diagnostics = new SsoLoginDiagnostics(logger) { Stage = SsoLoginStage.PushedAuthorizationRequest };
        diagnostics.ReportFailure(
            new InvalidOperationException("IDX21338: invalid_scope; private-response-and-token"),
            Guid.NewGuid(), 1);
        Assert.Contains("IDX21338", logger.Message);
        Assert.Contains("invalid_scope", logger.Message);
        Assert.DoesNotContain("private-response-and-token", logger.Message);
        Assert.Null(logger.Exception);
    }

    private sealed class CaptureLogger : ILogger<SsoLoginDiagnostics>
    {
        public string Message { get; private set; } = "";
        public Exception? Exception { get; private set; }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Message = formatter(state, exception);
            Exception = exception;
        }
    }
}
