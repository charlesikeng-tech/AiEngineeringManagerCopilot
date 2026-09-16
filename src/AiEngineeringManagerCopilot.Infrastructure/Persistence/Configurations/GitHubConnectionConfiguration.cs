using AiEngineeringManagerCopilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiEngineeringManagerCopilot.Infrastructure.Persistence.Configurations;

public sealed class GitHubConnectionConfiguration
    : IEntityTypeConfiguration<GitHubConnection>
{
    public void Configure(
        EntityTypeBuilder<GitHubConnection> builder)
    {
        builder.ToTable("github_connections");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.TeamId)
            .IsRequired();

        builder.Property(x => x.Organization)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.AccessTokenEncrypted)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.LastSyncAt);

        builder.HasIndex(x => x.TeamId)
            .IsUnique();
    }
}