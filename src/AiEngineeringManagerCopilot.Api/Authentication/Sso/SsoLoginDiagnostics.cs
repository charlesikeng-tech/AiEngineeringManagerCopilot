using System.Text.RegularExpressions;

namespace AiEngineeringManagerCopilot.Api.Authentication.Sso;

public enum SsoLoginStage
{
    Deployment,
    ActiveSnapshot,
    Backchannel,
    Options,
    SecretDecryption,
    HandlerInitialization,
    AuthorizationChallenge,
    PushedAuthorizationRequest,
    AuthorizationRedirect
}

public sealed class SsoLoginDiagnostics(ILogger<SsoLoginDiagnostics> logger)
{
    public SsoLoginStage Stage { get; set; } = SsoLoginStage.Deployment;

    public string ReportFailure(Exception exception, Guid providerId, int activeRevision)
    {
        var diagnosticId = Guid.NewGuid().ToString("N");
        var types = new List<string>();
        int? httpStatus = null;
        string? protocolError = null;
        string? identityModelCode = null;
        for (var current = exception; current is not null && types.Count < 8; current = current.InnerException)
        {
            types.Add(current.GetType().FullName ?? current.GetType().Name);
            if (current is HttpRequestException { StatusCode: { } status })
                httpStatus = (int)status;
            var code = Regex.Match(current.Message, @"\bIDX[0-9]{5}\b").Value;
            if (code.Length > 0) identityModelCode ??= code;
            foreach (var knownError in KnownProtocolErrors)
                if (current.Message.Contains(knownError, StringComparison.Ordinal))
                    protocolError ??= knownError;
        }

        // Exception messages, stacks and protocol payloads can contain credentials or tokens.
        logger.LogWarning(
            "SSO login preparation failed. DiagnosticId={DiagnosticId} ProviderId={ProviderId} ActiveRevision={ActiveRevision} Stage={Stage} ExceptionTypes={ExceptionTypes} HttpStatus={HttpStatus} IdentityModelCode={IdentityModelCode} ProtocolErrorHint={ProtocolErrorHint}",
            diagnosticId, providerId, activeRevision, Stage, string.Join(" -> ", types), httpStatus,
            identityModelCode, protocolError);
        return diagnosticId;
    }

    private static readonly string[] KnownProtocolErrors =
    [
        "invalid_client", "invalid_scope", "unauthorized_client", "access_denied",
        "invalid_request", "invalid_grant", "unsupported_response_type",
        "unsupported_grant_type", "server_error", "temporarily_unavailable"
    ];
}
