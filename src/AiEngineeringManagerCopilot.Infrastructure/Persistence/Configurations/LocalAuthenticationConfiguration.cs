using AiEngineeringManagerCopilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiEngineeringManagerCopilot.Infrastructure.Persistence.Configurations;

public sealed class LocalAuthenticationConfiguration :
    IEntityTypeConfiguration<LocalAdministrator>,
    IEntityTypeConfiguration<Installation>,
    IEntityTypeConfiguration<AdministratorSession>
{
    public void Configure(EntityTypeBuilder<LocalAdministrator> builder)
    {
        builder.HasKey(x => x.UserId);
        builder.Property(x => x.NormalizedEmail).HasMaxLength(254);
        builder.HasIndex(x => x.NormalizedEmail).IsUnique();
        builder.Property(x => x.PasswordHash).HasMaxLength(1024);
        builder.Property(x => x.Role).HasMaxLength(64);
        builder.HasOne<User>().WithOne().HasForeignKey<LocalAdministrator>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<Installation> builder)
    {
        builder.ToTable("Installations", table => table.HasCheckConstraint("CK_Installation_Singleton", "\"Id\" = 1"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
    }

    public void Configure(EntityTypeBuilder<AdministratorSession> builder)
    {
        builder.HasKey(x => x.TokenHash);
        builder.Property(x => x.TokenHash).HasMaxLength(64);
        builder.HasIndex(x => x.ExpiresAt);
        builder.HasOne<LocalAdministrator>().WithMany().HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
