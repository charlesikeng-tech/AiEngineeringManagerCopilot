using AiEngineeringManagerCopilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiEngineeringManagerCopilot.Infrastructure.Persistence.Configurations;

public sealed class JiraConnectionConfiguration
    : IEntityTypeConfiguration<JiraConnection>
{
    public void Configure(
        EntityTypeBuilder<JiraConnection> builder)
    {
        builder.ToTable("jira_connections");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.TeamId)
            .IsRequired();

        builder.Property(x => x.BaseUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(320);

        builder.Property(x => x.ApiTokenEncrypted)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.LastSyncAt);

        builder.HasIndex(x => x.TeamId)
            .IsUnique();
        
        builder.Property(x => x.ProjectKey)
            .IsRequired()
            .HasMaxLength(50);
    }
}