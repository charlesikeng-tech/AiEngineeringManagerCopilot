using AiEngineeringManagerCopilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiEngineeringManagerCopilot.Infrastructure.Persistence.Configurations;

public sealed class JiraWorkItemConfiguration
    : IEntityTypeConfiguration<JiraWorkItem>
{
    public void Configure(
        EntityTypeBuilder<JiraWorkItem> builder)
    {
        builder.ToTable("jira_work_items");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.TeamId)
            .IsRequired();

        builder.Property(x => x.ExternalId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Key)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Summary)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.AssigneeExternalId)
            .HasMaxLength(200);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.DoneAt);

        builder.Property(x => x.IsBlocked)
            .IsRequired();

        builder.HasIndex(x => new
            {
                x.TeamId,
                x.ExternalId
            })
            .IsUnique();

        builder.HasIndex(x => new
            {
                x.TeamId,
                x.Key
            })
            .IsUnique();
    }
}