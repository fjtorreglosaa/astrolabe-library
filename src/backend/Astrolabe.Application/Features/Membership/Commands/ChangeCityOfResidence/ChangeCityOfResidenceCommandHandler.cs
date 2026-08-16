using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Domain.Features.Identity.Repositories;
using Astrolabe.Domain.Features.Membership.Errors;
using Astrolabe.Domain.Features.Membership.Repositories;
using Astrolabe.Domain.Features.Network.Errors;
using Astrolabe.Domain.Features.Network.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Membership.Commands.ChangeCityOfResidence;

/// <summary>
/// Spans three contexts on purpose: the city is identity's, the allowance is membership's, and the
/// destination must be a real city with a home library, which is network's. The allowance is
/// consumed and the residence written in one transaction, so a failure cannot leave a member who
/// spent their change without moving.
/// </summary>
public sealed class ChangeCityOfResidenceCommandHandler(
    IIdentityUnitOfWork identity,
    IMembershipUnitOfWork membership,
    INetworkUnitOfWork network,
    ICurrentUser currentUser) : ICommandHandler<ChangeCityOfResidenceCommand>
{
    public async Task<Result> Handle(
        ChangeCityOfResidenceCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } memberId)
        {
            return Result.Failure(MembershipErrors.SubscriptionNotFound);
        }

        var city = await network.Cities.GetByIdAsync(request.CityId, cancellationToken);

        if (city is null || city.CountryId != request.CountryId)
        {
            return Result.Failure(NetworkErrors.CityNotFound);
        }

        // BR-NET-004 in reverse: a member must never end up in a city they cannot borrow from.
        if (city.HomeLibraryId is null)
        {
            return Result.Failure(NetworkErrors.CityNotFound);
        }

        var user = await identity.Users.GetByIdAsync(memberId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(MembershipErrors.SubscriptionNotFound);
        }

        var subscription = await membership.Subscriptions
            .GetActiveForMemberAsync(memberId, cancellationToken);

        if (subscription is null)
        {
            return Result.Failure(MembershipErrors.SubscriptionNotFound);
        }

        // Moving to the city already lived in is a no-op, not a spent allowance. Charging for it
        // would let a mis-click cost a member their one move for the cycle.
        if (user.CityId == request.CityId)
        {
            return Result.Success();
        }

        var allowance = subscription.RecordCityChange();

        if (allowance.IsFailure)
        {
            return allowance;
        }

        user.ChangeResidence(request.CountryId, request.CityId);

        // Both contexts share one DbContext, so a single transaction covers both saves. Saving them
        // independently could spend the allowance without moving the member.
        await membership.ExecuteInTransactionAsync(async ct =>
        {
            await identity.SaveChangesAsync(ct);
            await membership.SaveChangesAsync(ct);
        }, cancellationToken);

        return Result.Success();
    }
}
