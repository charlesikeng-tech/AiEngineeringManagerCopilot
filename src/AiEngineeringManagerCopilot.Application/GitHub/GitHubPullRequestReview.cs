namespace AiEngineeringManagerCopilot.Application.GitHub;

public sealed record GitHubPullRequestReview(
    long Id,
    string ReviewerExternalId,
    string State,
    DateTimeOffset SubmittedAt);