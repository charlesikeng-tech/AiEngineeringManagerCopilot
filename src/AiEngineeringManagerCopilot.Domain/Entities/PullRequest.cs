using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Domain.Entities;

public sealed class PullRequest
{
    public Guid Id { get; set; }

    public Guid RepositoryId { get; set; }

    public long ExternalId { get; set; }

    public string AuthorExternalId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public PullRequestState State { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? MergedAt { get; set; }

    public DateTimeOffset? ClosedAt { get; set; }
    
    public bool IsBlocked { get; set; }
}