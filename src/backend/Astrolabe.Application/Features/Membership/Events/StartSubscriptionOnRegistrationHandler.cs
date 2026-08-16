using Astrolabe.Application.Abstractions.Events;
using Astrolabe.Domain.Features.Identity.Events;
using Astrolabe.Domain.Features.Membership.Entities;
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
public sealed class StartSubscriptionOnRegistrationHandler(IMembershipUnitOfWork membership)
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

        // No check that the subject is a member, and no user lookup to make one: this event is
        // raised from exactly one place, User.Register, which since GLOBAL-019 always produces a
        // UserRole.Member. Staff arrive through Invite, which raises nothing. Re-reading the user
        // to re-assert what the constructor guarantees would buy a query and no safety.
        //
        // The plan travels on the event because it lives nowhere else between the visitor choosing
        // it and this subscription recording it. From this moment the subscription is the authority.
        var subscription = Subscription.Start(
            memberId, notification.DomainEvent.Plan, notification.DomainEvent.OccurredAt);

        await membership.Subscriptions.AddAsync(subscription, cancellationToken);
        await membership.SaveChangesAsync(cancellationToken);
    }
}
