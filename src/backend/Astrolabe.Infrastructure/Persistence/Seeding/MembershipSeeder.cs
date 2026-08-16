using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Membership.Entities;
using Astrolabe.Domain.Features.Membership.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Astrolabe.Infrastructure.Persistence.Seeding;

/// <summary>
/// Opens a subscription for every member that lacks one.
///
/// <para>
/// A backfill, not demo data, so it runs in every environment: members registered before the
/// membership domain existed have no subscription, and neither do the seeded demo accounts, whose
/// events are cleared before insert. Without a subscription a member's entitlement resolves to
/// nothing, and they would see an empty membership screen rather than the plan they signed up for.
/// </para>
/// <para>
/// Idempotent, and starts every backfilled member on <see cref="PlanTier.Basic"/>. Until
/// GLOBAL-019 this read the tier out of the member's role; roles no longer carry one, and there is
/// no other record of what an unsubscribed member bought — so the free tier is the only honest
/// answer. Granting a paid plan on a guess would hand out entitlements nobody paid for, while Basic
/// is recoverable: an upgrade is one screen away and charges correctly.
/// </para>
/// </summary>
public sealed class MembershipSeeder(
    AstrolabeDbContext context,
    ILogger<MembershipSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        var subscribedMemberIds = context.Subscriptions
            .Where(s => s.EndedAt == null)
            .Select(s => s.MemberId);

        var unsubscribed = await context.Users
            .Where(u => u.Role == UserRole.Member && !subscribedMemberIds.Contains(u.Id))
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        if (unsubscribed.Count == 0)
        {
            logger.LogInformation("Every member already holds a subscription. Nothing inserted.");
            return;
        }

        foreach (var memberId in unsubscribed)
        {
            var subscription = Subscription.Start(memberId, PlanTier.Basic, now);

            // The event exists to trigger billing and audit for a real sign-up. A backfill is
            // neither, so it is cleared rather than dispatched into handlers that would charge.
            subscription.ClearDomainEvents();

            context.Subscriptions.Add(subscription);
        }

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Opened {Count} subscription(s) for members that had none.", unsubscribed.Count);
    }
}
