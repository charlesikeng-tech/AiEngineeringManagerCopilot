using AiEngineeringManagerCopilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiEngineeringManagerCopilot.Infrastructure.Persistence.Configurations;

public sealed class PullRequestReviewConfiguration
    : IEntityTypeConfiguration<PullRequestReview>
{
    public void Configure(EntityTypeBuilder<PullRequestReview> builder)
    {
        builder.ToTable("pull_request_reviews");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.PullRequestId)
            .IsRequired();

        builder.Property(x => x.ReviewerExternalId)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.SubmittedAt)
            .IsRequired();

        builder.Property(x => x.State)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(x => x.PullRequestId);

        builder.HasIndex(x => x.SubmittedAt);

        builder.HasOne<PullRequest>()
            .WithMany()
            .HasForeignKey(x => x.PullRequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}