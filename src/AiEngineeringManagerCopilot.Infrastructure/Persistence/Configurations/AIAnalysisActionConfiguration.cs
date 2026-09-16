using AiEngineeringManagerCopilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiEngineeringManagerCopilot.Infrastructure.Persistence.Configurations;

public sealed class AIAnalysisActionConfiguration
    : IEntityTypeConfiguration<AIAnalysisAction>
{
    public void Configure(
        EntityTypeBuilder<AIAnalysisAction> builder)
    {
        builder.ToTable("ai_analysis_actions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title)
            .IsRequired();

        builder.Property(x => x.Description)
            .IsRequired();

        builder.Property(x => x.Priority)
            .IsRequired();

        builder.HasIndex(x => x.AIAnalysisId);
    }
}