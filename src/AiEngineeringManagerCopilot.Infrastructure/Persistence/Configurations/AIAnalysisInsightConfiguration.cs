using AiEngineeringManagerCopilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiEngineeringManagerCopilot.Infrastructure.Persistence.Configurations;

public sealed class AIAnalysisInsightConfiguration
    : IEntityTypeConfiguration<AIAnalysisInsight>
{
    public void Configure(
        EntityTypeBuilder<AIAnalysisInsight> builder)
    {
        builder.ToTable("ai_analysis_insights");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Category)
            .IsRequired();

        builder.Property(x => x.Title)
            .IsRequired();

        builder.Property(x => x.Description)
            .IsRequired();

        builder.Property(x => x.Impact)
            .IsRequired();

        builder.Property(x => x.Recommendation)
            .IsRequired();

        builder.HasIndex(x => x.AIAnalysisId);
    }
}