using AiEngineeringManagerCopilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiEngineeringManagerCopilot.Infrastructure.Persistence.Configurations;

public sealed class EngineeringRiskConfiguration
    : IEntityTypeConfiguration<EngineeringRisk>
{
    public void Configure(EntityTypeBuilder<EngineeringRisk> builder)
    {
        builder.ToTable("engineering_risks");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.TeamId)
            .IsRequired();

        builder.Property(x => x.ReportId)
            .IsRequired();

        builder.Property(x => x.MetricType)
            .HasConversion<string>()
            .HasMaxLength(100)
            .IsRequired(false);
        
        builder.Property(x => x.Severity)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Category)
            .HasConversion<string>()
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Title)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(5000)
            .IsRequired();

        builder.Property(x => x.Recommendation)
            .HasMaxLength(5000)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => x.TeamId);

        builder.HasIndex(x => x.ReportId);

        builder.HasOne<Team>()
            .WithMany()
            .HasForeignKey(x => x.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<EngineeringReport>()
            .WithMany()
            .HasForeignKey(x => x.ReportId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}