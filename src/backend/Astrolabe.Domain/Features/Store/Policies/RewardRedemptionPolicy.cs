using Astrolabe.Domain.Features.Membership.Enums;
using Astrolabe.Domain.Features.Store.Errors;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Store.Policies;

/// <summary>
/// How many reward points a member may put toward a purchase. Implements BR-STR-007.
///
/// <para>
/// One point-cent is one cent. The two units are kept apart in the vocabulary because they are
/// earned and spent by different rules, but the exchange is deliberately 1:1 — a rate would be a
/// second place for money arithmetic to drift, and the prototype's own copy reads
/// <c>3,240 pts · redeemable</c>, which only makes sense at parity.
/// </para>
/// </summary>
public static class RewardRedemptionPolicy
{
    /// <summary>
    /// The share of a purchase points may cover. BR-STR-007.
    ///
    /// <para>
    /// At the earning rate of one point-cent per $1.50, a member would need roughly seventy-five
    /// orders' worth of spending to reach this ceiling on a single order — so it will almost never
    /// bind, which is the point. It bounds the pathological case, a long-dormant balance emptied
    /// into one purchase, without touching the ordinary one.
    /// </para>
    /// </summary>
    public const int MaxPercentOfBookTotal = 50;

    /// <summary>
    /// The smallest redemption worth making. BR-STR-007.
    ///
    /// <para>
    /// A three-cent redemption costs a movement row, a ledger line and a line on the receipt to save
    /// three cents. The floor is the smallest amount that reads as money to a member.
    /// </para>
    /// </summary>
    public const int MinimumRedemptionPointCents = 100;

    /// <summary>
    /// The most this order could absorb, before the member's balance is considered.
    ///
    /// <para>
    /// Measured on the book total <b>after the plan discount and before delivery</b>. After the
    /// discount, because the discount is an entitlement the member already holds and letting points
    /// go first would quietly shrink what their plan is worth. Before delivery, because delivery is
    /// a cost passed straight through — points reward buying books, not choosing a courier.
    /// </para>
    /// <para>
    /// The same base <c>BR-STR-006</c> earns on, so earning and spending cannot drift apart.
    /// </para>
    /// </summary>
    public static int CapFor(Money afterDiscountBookTotal) =>
        afterDiscountBookTotal.Cents <= 0
            ? 0
            : (int)(afterDiscountBookTotal.Cents * MaxPercentOfBookTotal / 100);

    /// <summary>
    /// Whether this plan may spend points at all. BR-STR-008.
    ///
    /// <para>
    /// Earning and spending are both Max privileges, and a downgrade takes the second away with the
    /// first while leaving the balance untouched. That is deliberate rather than mean: a banked
    /// balance a member cannot reach is exactly what brings them back to Max, and BR-STR-008 says
    /// so in as many words. The balance is never forfeited.
    /// </para>
    /// </summary>
    public static bool CanRedeemOn(PlanTier plan) => plan is PlanTier.Max;

    /// <summary>
    /// The most this member may actually apply: the cap, their balance, whichever is smaller — and
    /// zero when that lands below the minimum, because offering someone 40 point-cents they are not
    /// allowed to spend is worse than offering nothing.
    /// </summary>
    public static int MaxRedeemable(PlanTier plan, int balancePointCents, Money afterDiscountBookTotal)
    {
        if (!CanRedeemOn(plan))
        {
            return 0;
        }

        var allowed = Math.Min(Math.Max(balancePointCents, 0), CapFor(afterDiscountBookTotal));

        return allowed < MinimumRedemptionPointCents ? 0 : allowed;
    }

    /// <summary>
    /// Judges a requested redemption. Zero is always valid — it simply means paying with money.
    /// </summary>
    public static Result EnsureValid(
        PlanTier plan, int requestedPointCents, int balancePointCents, Money afterDiscountBookTotal)
    {
        if (requestedPointCents == 0)
        {
            return Result.Success();
        }

        if (!CanRedeemOn(plan))
        {
            return Result.Failure(StoreErrors.RedemptionRequiresMaxPlan);
        }

        if (requestedPointCents < 0)
        {
            return Result.Failure(StoreErrors.RedemptionInvalid);
        }

        if (requestedPointCents < MinimumRedemptionPointCents)
        {
            return Result.Failure(StoreErrors.RedemptionBelowMinimum);
        }

        if (requestedPointCents > balancePointCents)
        {
            return Result.Failure(StoreErrors.RedemptionExceedsBalance);
        }

        if (requestedPointCents > CapFor(afterDiscountBookTotal))
        {
            return Result.Failure(StoreErrors.RedemptionExceedsCap);
        }

        return Result.Success();
    }
}
