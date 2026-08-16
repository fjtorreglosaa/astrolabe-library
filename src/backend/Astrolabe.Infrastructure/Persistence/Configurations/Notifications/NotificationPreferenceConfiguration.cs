using Astrolabe.Domain.Features.Notifications.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Astrolabe.Infrastructure.Persistence.Configurations.Notifications;

public sealed class NotificationPreferenceConfiguration
    : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.ToTable("notification_preferences");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.MemberId).IsRequired();
        builder.Property(p => p.Family).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(p => p.MutedAt).IsRequired();

        builder.Ignore(p => p.DomainEvents);

        // One row per member per family, and only for muted ones. The unique index is what stops a
        // double mute becoming two rows that disagree about nothing.
        builder.HasIndex(p => new { p.MemberId, p.Family }).IsUnique();
    }
}
