using AiEngineeringManagerCopilot.Application.Jira;

namespace AiEngineeringManagerCopilot.IntegrationTests.Fakes;

public sealed class FakeJiraClient : IJiraClient
{
    public JiraCurrentUser? CurrentUser { get; set; }

    public string? ReceivedBaseUrl { get; private set; }

    public string? ReceivedEmail { get; private set; }

    public string? ReceivedApiToken { get; private set; }
    
    public IReadOnlyList<JiraIssue> Issues { get; set; } = [];

    public string? ReceivedProjectKey { get; private set; }
    
    public Exception? ExceptionToThrow { get; set; }

    public Task<JiraCurrentUser?> GetCurrentUserAsync(
        string baseUrl,
        string email,
        string apiToken,
        CancellationToken cancellationToken)
    {
        ReceivedBaseUrl = baseUrl;
        ReceivedEmail = email;
        ReceivedApiToken = apiToken;

        return Task.FromResult(CurrentUser);
    }

    public Task<IReadOnlyList<JiraIssue>> GetIssuesAsync(
        string baseUrl,
        string email,
        string apiToken,
        string projectKey,
        CancellationToken cancellationToken)
    {
        
        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }
        
        ReceivedBaseUrl = baseUrl;
        ReceivedEmail = email;
        ReceivedApiToken = apiToken;
        ReceivedProjectKey = projectKey;

        return Task.FromResult(Issues);
    }

    public void Reset()
    {
        CurrentUser = null;
        Issues = [];

        ReceivedBaseUrl = null;
        ReceivedEmail = null;
        ReceivedApiToken = null;
        ReceivedProjectKey = null;
        
        ExceptionToThrow = null;
    }
}