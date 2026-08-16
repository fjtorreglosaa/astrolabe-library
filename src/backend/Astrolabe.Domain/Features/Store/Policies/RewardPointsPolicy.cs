using Astrolabe.Domain.Features.Membership.Enums;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Store.Policies;

/// <summary>
/// What an order earns in reward points. Implements BR-STR-005 and BR-STR-006.
///
/// <para>
/// Earned on the <b>post-discount</b> total, as <c>BR-STR-006</c> states: the member earns on what
/// they actually paid. Earning on the list price would effectively pay the discount twice.
/// </para>
/// <para>
/// <b>PLAN-001's example disagrees with the rule</b> and is recorded in
/// <c>store.business.md</c> §8. It says $150 at 15% accrues $1.00, which is $150 ÷ $1.50 with no
/// discount applied; the rule as written gives 85 point-cents. This follows the rule. If the plan's
/// figure is the intended one, the change is the single line below and its tests.
/// </para>
/// </summary>
public static class RewardPointsPolicy
{
    /// <summary>$1.50 of spend earns one point-cent. BR-STR-006.</summary>
    public static readonly Money SpendPerPointCent = Money.FromUnits(1, 50);

    /// <summary>
    /// Point-cents earned. Truncated downward, so a member is never credited for value they did not
    /// spend — the rounding always favours the library, which is the safe direction for a liability.
    /// </summary>
    public static int Earned(PlanTier plan, Money postDiscountTotal)
    {
        // BR-STR-005. Only Max accrues, and a plan check here means no caller can forget it.
        if (plan is not PlanTier.Max || postDiscountTotal.Cents <= 0)
        {
            return 0;
        }

        return (int)(postDiscountTotal.Cents / SpendPerPointCent.Cents);
    }
}
