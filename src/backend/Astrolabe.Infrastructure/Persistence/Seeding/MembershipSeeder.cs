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
/// Idempotent, and starts each member at the plan their role records — the one place the role is
/// read as the authority, because before a subscription exists it is the only record of the plan.
/// </para>
/// </summary>
public sealed class MembershipSeeder(
    AstrolabeDbContext context,
    ILogger<MembershipSeeder> logger)
{
    private static readonly UserRole[] MemberRoles = [UserRole.Basic, UserRole.Plus, UserRole.Max];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        var subscribedMemberIds = context.Subscriptions
            .Where(s => s.EndedAt == null)
            .Select(s => s.MemberId);

        var unsubscribed = await context.Users
            .Where(u => MemberRoles.Contains(u.Role) && !subscribedMemberIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Role })
            .ToListAsync(cancellationToken);

        if (unsubscribed.Count == 0)
        {
            logger.LogInformation("Every member already holds a subscription. Nothing inserted.");
            return;
        }

        foreach (var member in unsubscribed)
        {
            var subscription = Subscription.Start(member.Id, PlanTierFrom(member.Role), now);

            // The event exists to trigger billing and audit for a real sign-up. A backfill is
            // neither, so it is cleared rather than dispatched into handlers that would charge.
            subscription.ClearDomainEvents();

            context.Subscriptions.Add(subscription);
        }

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Opened {Count} subscription(s) for members that had none.", unsubscribed.Count);
    }

    private static PlanTier PlanTierFrom(UserRole role) => role switch
    {
        UserRole.Plus => PlanTier.Plus,
        UserRole.Max => PlanTier.Max,
        _ => PlanTier.Basic
    };
}
