using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Membership.Errors;
using Astrolabe.Domain.Features.Membership.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Membership.Commands.CancelScheduledPlanChange;

public sealed class CancelScheduledPlanChangeCommandHandler(
    IMembershipUnitOfWork membership,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : ICommandHandler<CancelScheduledPlanChangeCommand>
{
    public async Task<Result> Handle(
        CancelScheduledPlanChangeCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } memberId)
        {
            return Result.Failure(MembershipErrors.SubscriptionNotFound);
        }

        var subscription = await membership.Subscriptions
            .GetActiveForMemberAsync(memberId, cancellationToken);

        if (subscription is null)
        {
            return Result.Failure(MembershipErrors.SubscriptionNotFound);
        }

        var result = subscription.CancelScheduledChange(clock.UtcNow);

        if (result.IsFailure)
        {
            return result;
        }

        await membership.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
