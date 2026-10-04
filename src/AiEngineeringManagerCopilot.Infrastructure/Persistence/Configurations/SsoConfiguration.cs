using AiEngineeringManagerCopilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiEngineeringManagerCopilot.Infrastructure.Persistence.Configurations;

public sealed class SsoProviderConfiguration : IEntityTypeConfiguration<SsoProvider>
{
    public void Configure(EntityTypeBuilder<SsoProvider> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Type).HasMaxLength(20);
        builder.Property(x => x.Name).HasMaxLength(200);
        builder.Property(x => x.Authority).HasMaxLength(512);
        builder.Property(x => x.ClientId).HasMaxLength(256);
        builder.Property(x => x.LastTestError).HasMaxLength(100);
        builder.Property(x => x.Revision).IsConcurrencyToken();
    }
}

public sealed class SsoConnectionTestConfiguration : IEntityTypeConfiguration<SsoConnectionTest>
{
    public void Configure(EntityTypeBuilder<SsoConnectionTest> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SessionHash).HasMaxLength(64);
        builder.HasOne<SsoProvider>().WithMany().HasForeignKey(x => x.ProviderId).OnDelete(DeleteBehavior.Cascade);
        // The hash can reference a local or approved SSO session; callback proof
        // checks the corresponding live table rather than a local-only FK.
        builder.HasIndex(x => x.SessionHash);
        builder.HasIndex(x => x.ExpiresAt);
    }
}
