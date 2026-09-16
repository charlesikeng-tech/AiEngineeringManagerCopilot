using AiEngineeringManagerCopilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiEngineeringManagerCopilot.Infrastructure.Persistence.Configurations;

public sealed class AIAnalysisConfiguration
    : IEntityTypeConfiguration<AIAnalysis>
{
    public void Configure(
        EntityTypeBuilder<AIAnalysis> builder)
    {
        builder.ToTable("ai_analyses");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Summary)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => x.ReportId);
    }
}