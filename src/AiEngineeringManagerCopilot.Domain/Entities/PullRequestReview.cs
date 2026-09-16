using AiEngineeringManagerCopilot.Domain.Enums;

namespace AiEngineeringManagerCopilot.Domain.Entities;

public sealed class PullRequestReview
{
    public Guid Id { get; set; }

    public Guid PullRequestId { get; set; }

    public string ReviewerExternalId { get; set; } = string.Empty;

    public DateTimeOffset SubmittedAt { get; set; }
    
    public PullRequestReviewState State { get; set; }
}