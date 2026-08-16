using Astrolabe.Application.Abstractions.Events;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Identity.Events;
using Astrolabe.Domain.Features.Identity.Repositories;
using Astrolabe.Domain.Features.Membership.Entities;
using Astrolabe.Domain.Features.Membership.Enums;
using Astrolabe.Domain.Features.Membership.Repositories;
using MediatR;

namespace Astrolabe.Application.Features.Membership.Events;

/// <summary>
/// Opens a subscription for a newly registered member, at the plan they chose. Implements the
/// starting half of BR-MBR-001.
///
/// <para>
/// Driven by the event rather than by the registration handler, so membership stays out of
/// identity's write path and a second way to register cannot forget to create the subscription.
/// Idempotent: a redelivered event finds the subscription already open and does nothing.
/// </para>
/// </summary>
public sealed class StartSubscriptionOnRegistrationHandler(
    IMembershipUnitOfWork membership,
    IUserRepository users)
    : INotificationHandler<DomainEventNotification<UserRegistered>>
{
    public async Task Handle(
        DomainEventNotification<UserRegistered> notification, CancellationToken cancellationToken)
    {
        var memberId = notification.DomainEvent.UserId;

        var existing = await membership.Subscriptions
            .GetActiveForMemberAsync(memberId, cancellationToken);

        if (existing is not null)
        {
            return;
        }

        var user = await users.GetByIdAsync(memberId, cancellationToken);

        // Staff arrive by invitation and hold no subscription. Guarding here rather than on the
        // event keeps the event a plain statement of fact.
        if (user is null || !user.Role.IsMember())
        {
            return;
        }

        var subscription = Subscription.Start(
            memberId, PlanTierFrom(user.Role), notification.DomainEvent.OccurredAt);

        await membership.Subscriptions.AddAsync(subscription, cancellationToken);
        await membership.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// A member's role <em>is</em> their plan at registration (global_spec.md §2). From here on the
    /// subscription is the authority and the role mirrors it, never the other way round.
    /// </summary>
    private static PlanTier PlanTierFrom(UserRole role) => role switch
    {
        UserRole.Plus => PlanTier.Plus,
        UserRole.Max => PlanTier.Max,
        _ => PlanTier.Basic
    };
}
