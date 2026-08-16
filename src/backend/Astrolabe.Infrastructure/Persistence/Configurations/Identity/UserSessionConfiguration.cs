using Astrolabe.Domain.Features.Identity.Entities;
using Astrolabe.Domain.Features.Identity.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Astrolabe.Infrastructure.Persistence.Configurations.Identity;

public sealed class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("user_sessions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.UserId).IsRequired();
        builder.Property(s => s.IpAddress).HasMaxLength(45).IsRequired();
        builder.Property(s => s.ApproximateLocation).HasMaxLength(120);
        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.LastSeenAt).IsRequired();
        builder.Property(s => s.ExpiresAt).IsRequired();
        builder.Property(s => s.RevokedAt);
        builder.Property(s => s.RevokedReason).HasConversion<int>();

        // The device label is a value object, so it lives in the session's own row rather than a
        // table of its own. It is display data (BR-IDN-022), never an authorization input.
        builder.OwnsOne(s => s.Device, device =>
        {
            device.Property(d => d.Name)
                .HasColumnName("device_name")
                .HasMaxLength(DeviceDescriptor.MaxNameLength)
                .IsRequired();

            device.Property(d => d.Type).HasColumnName("device_type").HasConversion<int>().IsRequired();

            device.Property(d => d.ClientDeviceId).HasColumnName("client_device_id").HasMaxLength(100);
        });

        builder.Navigation(s => s.Device).IsRequired();

        // The token chain belongs to the session and is always loaded with it: reuse detection
        // needs the rotated links, not just the live one.
        builder.HasMany(s => s.Tokens)
            .WithOne()
            .HasForeignKey(t => t.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(s => s.Tokens).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(s => s.DomainEvents);
        builder.Ignore(s => s.IsRevoked);

        // Optimistic concurrency through PostgreSQL's own system column. Two simultaneous rotations
        // must not both succeed: the loser surfaces as reuse, which is the correct reading.
        // A shadow property keeps the concurrency token out of the domain model entirely.
        // `UseXminAsConcurrencyToken` was removed in Npgsql 10; this is the supported form.
        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasIndex(s => new { s.UserId, s.RevokedAt });
    }
}
