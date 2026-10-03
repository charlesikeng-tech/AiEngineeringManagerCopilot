using AiEngineeringManagerCopilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiEngineeringManagerCopilot.Infrastructure.Persistence.Configurations;

public sealed class MicrosoftTeamsWebhookConnectionConfiguration
    : IEntityTypeConfiguration<MicrosoftTeamsWebhookConnection>
{
    public void Configure(EntityTypeBuilder<MicrosoftTeamsWebhookConnection> builder)
    {
        builder.ToTable("microsoft_teams_webhook_connections");
        builder.HasKey(connection => connection.Id);
        builder.Property(connection => connection.Id).ValueGeneratedNever();
        builder.Property(connection => connection.TeamId).IsRequired();
        builder.Property(connection => connection.WebhookUrlEncrypted).IsRequired();
        builder.Property(connection => connection.CreatedAt).IsRequired();
        builder.HasIndex(connection => connection.TeamId).IsUnique();
    }
}
