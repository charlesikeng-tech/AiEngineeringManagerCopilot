using AiEngineeringManagerCopilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiEngineeringManagerCopilot.Infrastructure.Persistence.Configurations;

public sealed class AIAnalysisEvidenceConfiguration
    : IEntityTypeConfiguration<AIAnalysisEvidence>
{
    public void Configure(EntityTypeBuilder<AIAnalysisEvidence> builder)
    {
        builder.ToTable("ai_analysis_evidences");
        
        builder.HasKey(x => x.Id);

        builder.Property(x => x.MetricType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Reason)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(x => x.Value)
            .HasPrecision(18, 4);

        builder.Property(x => x.Confidence)
            .HasPrecision(5, 4);

        builder.HasOne<AIAnalysis>()
            .WithMany(x => x.Evidence)
            .HasForeignKey(x => x.AIAnalysisId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}