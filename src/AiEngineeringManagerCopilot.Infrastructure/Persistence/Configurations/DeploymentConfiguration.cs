using AiEngineeringManagerCopilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiEngineeringManagerCopilot.Infrastructure.Persistence.Configurations;

public sealed class DeploymentConfiguration
    : IEntityTypeConfiguration<Deployment>
{
    public void Configure(
        EntityTypeBuilder<Deployment> builder)
    {
        builder.ToTable("deployments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.RepositoryId)
            .IsRequired();

        builder.Property(x => x.ExternalId)
            .IsRequired();

        builder.Property(x => x.Environment)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.DeployedAt)
            .IsRequired();

        builder.HasIndex(x => new
            {
                x.RepositoryId,
                x.ExternalId
            })
            .IsUnique();

        builder.HasIndex(x => x.DeployedAt);
    }
}