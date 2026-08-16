using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Membership;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Store;
using Astrolabe.Domain.Features.Membership.Enums;
using Astrolabe.Domain.Features.Store.Errors;
using Astrolabe.Domain.Features.Store.Policies;
using Astrolabe.Domain.Features.Store.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Store.Queries.GetMyPoints;

public sealed class GetMyPointsQueryHandler(
    IStoreUnitOfWork store,
    IEntitlementProvider entitlements,
    ICurrentUser currentUser) : IQueryHandler<GetMyPointsQuery, PointsSummaryDto>
{
    /// <summary>How much history the profile card shows.</summary>
    private const int RecentMovements = 10;

    public async Task<Result<PointsSummaryDto>> Handle(
        GetMyPointsQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } memberId)
        {
            return Result.Failure<PointsSummaryDto>(StoreErrors.OrderNotYours);
        }

        var member = await entitlements.GetForCurrentMemberAsync(cancellationToken);
        var balance = await store.Points.GetBalanceAsync(memberId, cancellationToken);
        var recent = await store.Points.GetForMemberAsync(memberId, RecentMovements, cancellationToken);

        var earns = member.Plan is PlanTier.Max;

        return Result.Success(new PointsSummaryDto(
            BalancePointCents: balance,
            EarnsPoints: earns,
            // BR-STR-007 and BR-STR-008 together: enough points to clear the floor, and an active
            // Max plan. The balance itself never expires — a downgrade suspends spending, it does
            // not forfeit anything.
            CanRedeem: RewardRedemptionPolicy.CanRedeemOn(member.Plan)
                && balance >= RewardRedemptionPolicy.MinimumRedemptionPointCents,
            Note: NoteFor(member.Plan, balance),
            Recent: recent
                .Select(movement => new PointsMovementDto(
                    movement.Id, movement.PointCents, movement.Description, movement.OccurredAt))
                .ToList()));
    }

    /// <summary>
    /// BR-STR-008: points survive a downgrade. A member who banked them on Max and moved down keeps
    /// both the balance and the right to spend it, and must be told so plainly.
    /// </summary>
    private static string NoteFor(PlanTier plan, int balance)
    {
        var spendable = balance >= RewardRedemptionPolicy.MinimumRedemptionPointCents;

        return plan switch
        {
            PlanTier.Max when spendable =>
                "You earn a point for every $1.50 on books, and points cover up to half a purchase.",
            PlanTier.Max when balance > 0 =>
                $"{RewardRedemptionPolicy.MinimumRedemptionPointCents} points and you can start "
                + "spending them. You earn one for every $1.50 on books.",
            PlanTier.Max => "Buy a book and you start earning. A point for every $1.50 you spend.",

            // BR-STR-008: kept, not forfeited, and waiting on Max rather than lost to a downgrade.
            _ when balance > 0 =>
                "Your points are safe and never expire. Spending them needs the Max plan.",

            _ => "Reward points are a Max plan benefit."
        };
    }
}
