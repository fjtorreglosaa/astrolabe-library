using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Membership;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Membership.Enums;
using Astrolabe.Domain.Features.Membership.Errors;
using Astrolabe.Domain.Features.Membership.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Membership.Commands.ChangePlan;

public sealed class ChangePlanCommandHandler(
    IMembershipUnitOfWork membership,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : ICommandHandler<ChangePlanCommand, PlanChangeResultDto>
{
    public async Task<Result<PlanChangeResultDto>> Handle(
        ChangePlanCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } memberId)
        {
            return Result.Failure<PlanChangeResultDto>(MembershipErrors.SubscriptionNotFound);
        }

        var subscription = await membership.Subscriptions
            .GetActiveForMemberAsync(memberId, cancellationToken);

        if (subscription is null)
        {
            return Result.Failure<PlanChangeResultDto>(MembershipErrors.SubscriptionNotFound);
        }

        var now = clock.UtcNow;

        // Apply anything already due before deciding direction. Without this, a member whose
        // downgrade landed overnight would be quoted against the plan they no longer hold.
        subscription.ApplyDueChange(now);

        // Rank decides, never price. A future price change must not silently turn an upgrade into
        // a downgrade and charge for it.
        var isUpgrade = request.TargetPlan.IsHigherThan(subscription.Plan);

        if (isUpgrade)
        {
            // No payment is taken in this stage. The flag records that the product requires a card
            // on file for a paid plan; settlement itself is out of scope.
            var upgrade = subscription.Upgrade(request.TargetPlan, hasPaymentMethod: true, now);

            if (upgrade.IsFailure)
            {
                return Result.Failure<PlanChangeResultDto>(upgrade.Error);
            }

            await membership.SaveChangesAsync(cancellationToken);

            return Result.Success(new PlanChangeResultDto(
                Plan: subscription.Plan.ToString(),
                AppliedImmediately: true,
                AmountChargedCents: (int)upgrade.Value.AmountDue.Cents,
                EffectiveOn: now));
        }

        var scheduled = subscription.ScheduleDowngrade(request.TargetPlan, now);

        if (scheduled.IsFailure)
        {
            return Result.Failure<PlanChangeResultDto>(scheduled.Error);
        }

        await membership.SaveChangesAsync(cancellationToken);

        return Result.Success(new PlanChangeResultDto(
            // The plan reported is the one in force, not the one requested: BR-MBR-016 keeps the
            // member on what they paid for until the cycle ends.
            Plan: subscription.Plan.ToString(),
            AppliedImmediately: false,
            AmountChargedCents: 0,
            EffectiveOn: subscription.ScheduledChange!.EffectiveOn));
    }
}
