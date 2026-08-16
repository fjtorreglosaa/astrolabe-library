using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Membership;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Store;
using Astrolabe.Domain.Features.Membership.Enums;
using Astrolabe.Domain.Features.Store.Errors;
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
            // BR-STR-007 is undefined and BLOCK-002 is open, so nobody can redeem. Sent as a field
            // rather than assumed by the client, so the interface follows the server the day the
            // rule is decided.
            CanRedeem: false,
            Note: NoteFor(member.Plan, balance),
            Recent: recent
                .Select(movement => new PointsMovementDto(
                    movement.Id, movement.PointCents, movement.Description, movement.OccurredAt))
                .ToList()));
    }

    /// <summary>
    /// BR-STR-008: points survive a downgrade. A member who banked them on Max and moved down must
    /// be told they still have them, not shown a balance with no explanation.
    /// </summary>
    private static string NoteFor(PlanTier plan, int balance) => plan switch
    {
        PlanTier.Max when balance > 0 => "You earn a point on every $1.50 you spend on books.",
        PlanTier.Max => "Buy a book and you start earning. A point for every $1.50 you spend.",

        _ when balance > 0 =>
            "Your points are safe. Redeeming them needs the Max plan, and redemption is not open yet.",

        _ => "Reward points are a Max plan benefit."
    };
}
