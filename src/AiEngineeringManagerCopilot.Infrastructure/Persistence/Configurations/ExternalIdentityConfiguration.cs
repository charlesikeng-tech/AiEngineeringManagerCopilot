using AiEngineeringManagerCopilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiEngineeringManagerCopilot.Infrastructure.Persistence.Configurations;

public sealed class ExternalIdentityConfiguration : IEntityTypeConfiguration<ExternalIdentity>
{
    public void Configure(EntityTypeBuilder<ExternalIdentity> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Issuer).HasMaxLength(512).IsRequired();
        builder.Property(x => x.Subject).HasMaxLength(255).IsRequired();
        builder.HasIndex(x => new { x.Issuer, x.Subject }).IsUnique();
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class SsoSessionConfiguration : IEntityTypeConfiguration<SsoSession>
{
    public void Configure(EntityTypeBuilder<SsoSession> builder)
    {
        builder.HasKey(x => x.TokenHash);
        builder.Property(x => x.TokenHash).HasMaxLength(64);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ExternalIdentity>().WithMany().HasForeignKey(x => x.ExternalIdentityId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<SsoProvider>().WithMany().HasForeignKey(x => x.ProviderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.ExpiresAt);
    }
}

public sealed class SsoLoginAttemptConfiguration : IEntityTypeConfiguration<SsoLoginAttempt>
{
    public void Configure(EntityTypeBuilder<SsoLoginAttempt> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PreviousSessionHash).HasMaxLength(64);
        builder.HasOne<SsoProvider>().WithMany().HasForeignKey(x => x.ProviderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.ExpiresAt);
    }
}
