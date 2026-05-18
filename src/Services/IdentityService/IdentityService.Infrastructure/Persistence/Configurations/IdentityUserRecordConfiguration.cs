using IdentityService.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityService.Infrastructure.Persistence.Configurations;

public sealed class IdentityUserRecordConfiguration : IEntityTypeConfiguration<IdentityUserRecord>
{
    public void Configure(EntityTypeBuilder<IdentityUserRecord> builder)
    {
        builder.ToTable("users");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.Email)
            .HasColumnName("email")
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(x => x.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(x => x.RolesCsv)
            .HasColumnName("roles")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(x => x.ScopesCsv)
            .HasColumnName("scopes")
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.HasIndex(x => x.Email)
            .IsUnique();
    }
}
