using Astrolabe.Domain.Features.Identity.Entities;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Identity.ValueObjects;
using Astrolabe.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Astrolabe.Infrastructure.Persistence.Configurations.Identity;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .HasConversion<EmailConverter>()
            .HasMaxLength(Email.MaxLength)
            .IsRequired();

        // Nullable: a staff account has no password until its invitation is accepted.
        builder.Property(u => u.PasswordHash)
            .HasConversion<PasswordHashConverter>()
            .HasMaxLength(256);

        builder.Property(u => u.FullName).HasMaxLength(200).IsRequired();

        builder.Property(u => u.Role).HasConversion<int>().IsRequired();
        builder.Property(u => u.Status).HasConversion<int>().IsRequired();

        builder.Property(u => u.CreatedAt).IsRequired();
        builder.Property(u => u.VerifiedAt);
        builder.Property(u => u.FailedSignInAttempts).IsRequired();
        builder.Property(u => u.LockedUntil);
        builder.Property(u => u.TotpSecret).HasMaxLength(128);

        builder.Property(u => u.CountryId);
        builder.Property(u => u.CityId);

        builder.Ignore(u => u.DomainEvents);

        // BR-IDN-002 enforced by the database. The filter excludes deleted accounts, matching
        // IUserRepository: an application-only check races between lookup and insert, so two
        // simultaneous registrations for the same address would both pass.
        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasFilter($"status <> {(int)UserStatus.Deleted}");

        builder.HasIndex(u => u.Status);
    }
}
