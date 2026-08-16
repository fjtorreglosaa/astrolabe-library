using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Membership;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Membership;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Membership.Errors;
using Astrolabe.Domain.Features.Membership.Repositories;
using Astrolabe.Domain.Features.Membership.ValueObjects;
using Astrolabe.Domain.Features.Network.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Membership.Queries.GetMyMembership;

public sealed class GetMyMembershipQueryHandler(
    IMembershipUnitOfWork membership,
    INetworkUnitOfWork network,
    IEntitlementProvider entitlements,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : IQueryHandler<GetMyMembershipQuery, MembershipDto>
{
    public async Task<Result<MembershipDto>> Handle(
        GetMyMembershipQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } memberId)
        {
            return Result.Failure<MembershipDto>(MembershipErrors.SubscriptionNotFound);
        }

        // The entitlement is resolved first: it already applies any due change and already resolves
        // the member's city and home library, so asking it here avoids repeating both.
        var entitlement = await entitlements.GetForCurrentMemberAsync(cancellationToken);

        var subscription = await membership.Subscriptions
            .GetActiveForMemberAsync(memberId, cancellationToken);

        if (subscription is null)
        {
            return Result.Failure<MembershipDto>(MembershipErrors.SubscriptionNotFound);
        }

        // Only the names are missing from the entitlement, which carries identifiers by design so
        // that the domains consuming it never pay for text they do not render.
        var city = entitlement.CityId is { } cityId
            ? await network.Cities.GetByIdAsync(cityId, cancellationToken)
            : null;
        var homeLibrary = entitlement.HomeLibraryId is { } homeLibraryId
            ? await network.Libraries.GetByIdAsync(homeLibraryId, cancellationToken)
            : null;

        var plan = PlanCatalog.For(subscription.Plan);

        return Result.Success(new MembershipDto(
            Plan: subscription.Plan.ToString(),
            Reach: plan.Reach.ToString(),
            PriceCents: (int)plan.MonthlyPrice.Cents,
            DiscountPercent: plan.DiscountPercent,
            EarnsPoints: plan.EarnsPoints,
            SeesRecommendations: plan.SeesRecommendations,
            CycleStartedOn: subscription.Cycle.StartedOn,
            RenewsOn: subscription.Cycle.RenewsOn,
            DaysRemaining: subscription.Cycle.DaysRemainingAt(clock.UtcNow),
            CityId: city?.Id,
            CityName: city?.Name,
            HomeLibraryId: homeLibrary?.Id,
            HomeLibraryName: homeLibrary?.Name,
            ScheduledChange: subscription.ScheduledChange is { } change
                ? new ScheduledPlanChangeDto(
                    change.Target.ToString(), change.EffectiveOn, change.RequestedAt)
                : null,
            CanChangeCityThisCycle: subscription.CityChangesThisCycle == 0));
    }
}
