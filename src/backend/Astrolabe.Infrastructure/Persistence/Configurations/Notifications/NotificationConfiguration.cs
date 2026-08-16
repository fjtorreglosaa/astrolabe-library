using Astrolabe.Domain.Features.Notifications.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Astrolabe.Infrastructure.Persistence.Configurations.Notifications;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.MemberId).IsRequired();
        builder.Property(n => n.Kind).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(n => n.Title).HasMaxLength(Notification.MaxTitleLength).IsRequired();
        builder.Property(n => n.Body).HasMaxLength(Notification.MaxBodyLength).IsRequired();
        builder.Property(n => n.Route).HasMaxLength(128);
        builder.Property(n => n.OccurredAt).IsRequired();
        builder.Property(n => n.ReadAt);

        builder.Ignore(n => n.IsRead);
        builder.Ignore(n => n.DomainEvents);

        // The feed reads by member, newest first, and the badge counts the unread among them.
        // Filtered on read_at so the count is an index scan rather than a table scan.
        builder.HasIndex(n => new { n.MemberId, n.OccurredAt });
        builder.HasIndex(n => n.MemberId).HasFilter("read_at IS NULL");
    }
}
