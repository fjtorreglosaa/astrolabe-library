using Astrolabe.Application.Abstractions.Events;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Identity.Repositories;
using Astrolabe.Domain.Features.Membership.Enums;
using Astrolabe.Domain.Features.Membership.Events;
using MediatR;

namespace Astrolabe.Application.Features.Membership.Events;

/// <summary>
/// Keeps <c>User.Role</c> in step with the subscription's plan.
///
/// <para>
/// A member's role doubles as their plan for display and for the token's claims, so the two would
/// drift the moment a plan changed. The subscription is the authority; this handler makes the mirror
/// structural, so no plan-changing path has to remember it. Reacting to both events matters:
/// an upgrade applies now, a downgrade applies at renewal, and only one of them is a user action.
/// </para>
/// <para>
/// Nothing is authorised on the plan portion of the role — every plan gate goes through
/// <c>IEntitlementProvider</c>, which reads the subscription — so a role claim that is stale for the
/// remaining life of an access token cannot grant anything the member has not paid for.
/// </para>
/// </summary>
public sealed class MirrorPlanOntoUserRoleHandler(
    IIdentityUnitOfWork identity)
    : INotificationHandler<DomainEventNotification<PlanUpgraded>>,
      INotificationHandler<DomainEventNotification<PlanChangeApplied>>
{
    public Task Handle(
        DomainEventNotification<PlanUpgraded> notification, CancellationToken cancellationToken) =>
        MirrorAsync(notification.DomainEvent.MemberId, notification.DomainEvent.To, cancellationToken);

    public Task Handle(
        DomainEventNotification<PlanChangeApplied> notification, CancellationToken cancellationToken) =>
        MirrorAsync(notification.DomainEvent.MemberId, notification.DomainEvent.To, cancellationToken);

    private async Task MirrorAsync(Guid memberId, PlanTier plan, CancellationToken cancellationToken)
    {
        var user = await identity.Users.GetByIdAsync(memberId, cancellationToken);

        // Staff hold no plan. A missing user means the account went away between the change and the
        // dispatch, which is not an error worth failing the commit for.
        if (user is null || !user.Role.IsMember())
        {
            return;
        }

        user.ChangePlan(RoleFrom(plan));

        await identity.SaveChangesAsync(cancellationToken);
    }

    private static UserRole RoleFrom(PlanTier plan) => plan switch
    {
        PlanTier.Plus => UserRole.Plus,
        PlanTier.Max => UserRole.Max,
        _ => UserRole.Basic
    };
}
