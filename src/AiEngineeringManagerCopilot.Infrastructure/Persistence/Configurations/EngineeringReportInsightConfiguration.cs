using AiEngineeringManagerCopilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiEngineeringManagerCopilot.Infrastructure.Persistence.Configurations;

public sealed class EngineeringReportInsightConfiguration
    : IEntityTypeConfiguration<EngineeringReportInsight>
{
    public void Configure(EntityTypeBuilder<EngineeringReportInsight> builder)
    {
        builder.ToTable("engineering_report_insights");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.ReportId)
            .IsRequired();

        builder.Property(x => x.Category)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Title)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(5000)
            .IsRequired();

        builder.Property(x => x.Impact)
            .HasMaxLength(3000)
            .IsRequired();

        builder.Property(x => x.Recommendation)
            .HasMaxLength(5000)
            .IsRequired();

        builder.HasIndex(x => x.ReportId);

        builder.HasOne<EngineeringReport>()
            .WithMany()
            .HasForeignKey(x => x.ReportId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}