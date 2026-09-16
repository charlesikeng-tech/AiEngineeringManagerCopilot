using AiEngineeringManagerCopilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiEngineeringManagerCopilot.Infrastructure.Persistence.Configurations;

public sealed class EngineeringReportConfiguration
    : IEntityTypeConfiguration<EngineeringReport>
{
    public void Configure(EntityTypeBuilder<EngineeringReport> builder)
    {
        builder.ToTable(
            "engineering_reports",
            t =>
            {
                t.HasCheckConstraint(
                    "CK_engineering_reports_overall_score",
                    "\"OverallScore\" >= 0 AND \"OverallScore\" <= 100");
            });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.TeamId)
            .IsRequired();

        builder.Property(x => x.PeriodStart)
            .IsRequired();

        builder.Property(x => x.PeriodEnd)
            .IsRequired();

        builder.Property(x => x.ExecutiveSummary)
            .HasMaxLength(10000)
            .IsRequired();

        builder.Property(x => x.OverallScore)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => x.TeamId);

        builder.HasIndex(x => new
        {
            x.TeamId,
            x.PeriodStart,
            x.PeriodEnd
        });

        builder.HasOne<Team>()
            .WithMany()
            .HasForeignKey(x => x.TeamId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}