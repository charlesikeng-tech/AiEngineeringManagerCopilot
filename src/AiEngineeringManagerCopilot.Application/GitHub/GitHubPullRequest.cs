namespace AiEngineeringManagerCopilot.Application.GitHub;

public sealed record GitHubPullRequest(
    long Id,
    int Number,
    string Title,
    string AuthorExternalId,
    string State,
    DateTimeOffset CreatedAt,
    DateTimeOffset? MergedAt,
    DateTimeOffset? ClosedAt);