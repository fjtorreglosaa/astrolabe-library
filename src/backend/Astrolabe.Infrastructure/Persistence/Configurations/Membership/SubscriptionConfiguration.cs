using Astrolabe.Domain.Features.Membership.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Astrolabe.Infrastructure.Persistence.Configurations.Membership;

public sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("subscriptions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.MemberId).IsRequired();
        builder.Property(s => s.Plan).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(s => s.StartedAt).IsRequired();
        builder.Property(s => s.EndedAt);
        builder.Property(s => s.CityChangesThisCycle).IsRequired();

        builder.Ignore(s => s.IsActive);
        builder.Ignore(s => s.DomainEvents);

        // The cycle is meaningless without its subscription and is never queried on its own, so it
        // stays owned rather than becoming a table nobody joins to independently.
        builder.OwnsOne(s => s.Cycle, cycle =>
        {
            cycle.Property(c => c.StartedOn).HasColumnName("cycle_started_on").IsRequired();
            cycle.Property(c => c.RenewsOn).HasColumnName("cycle_renews_on").IsRequired();

            // Stored, not derived from the renewal date. A cycle anchored on the 31st renews on
            // 28 February and must return to the 31st in March; deriving it walks the day backwards.
            cycle.Property(c => c.AnchorDay).HasColumnName("cycle_anchor_day").IsRequired();

            // The renewal sweep filters on this column. Filtered on ended_at rather than made
            // composite with it: an owned type cannot contribute to its owner's index, and the
            // filter gives the same selectivity because ended rows are never swept.
            cycle.HasIndex(c => c.RenewsOn).HasFilter("ended_at IS NULL");
        });
        builder.Navigation(s => s.Cycle).IsRequired();

        builder.OwnsOne(s => s.ScheduledChange, change =>
        {
            change.Property(c => c.Target)
                .HasColumnName("scheduled_change_target").HasConversion<string>().HasMaxLength(16);
            change.Property(c => c.EffectiveOn).HasColumnName("scheduled_change_effective_on");
            change.Property(c => c.RequestedAt).HasColumnName("scheduled_change_requested_at");
        });

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        // One active subscription per member. Ended rows are excluded so a member who resubscribes
        // keeps their history instead of overwriting it.
        builder.HasIndex(s => s.MemberId)
            .IsUnique()
            .HasFilter("ended_at IS NULL");
    }
}
