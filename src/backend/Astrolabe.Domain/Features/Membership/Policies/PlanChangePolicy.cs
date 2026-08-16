using Astrolabe.Domain.Features.Membership.Enums;

namespace Astrolabe.Domain.Features.Membership.Policies;

/// <summary>
/// What a member is told they give up by moving between two plans. Implements BR-MBR-020.
///
/// <para>
/// A pure static function over two tiers: no repository, no clock, no member. That is what lets
/// every transition be exercised as fast unit tests, and it keeps the answer identical whether it
/// is asked by the quote query or by the confirmation itself.
/// </para>
/// </summary>
public static class PlanChangePolicy
{
    public static IReadOnlyList<PlanChangeLoss> LossesOf(PlanTier from, PlanTier to)
    {
        // Moving up costs nothing in entitlements. Returning an empty list rather than refusing
        // keeps the caller from having to branch on direction before asking.
        if (!from.IsHigherThan(to))
        {
            return [];
        }

        var losses = new List<PlanChangeLoss>();

        // Only Max accrues points, so only leaving Max can cost them.
        if (from is PlanTier.Max)
        {
            losses.Add(PlanChangeLoss.RewardPoints);
        }

        // Both remaining losses are properties of arriving at Basic, not of the plan being left.
        if (to is PlanTier.Basic)
        {
            losses.Add(PlanChangeLoss.HomeLibraryAndBasicCatalog);
            losses.Add(PlanChangeLoss.Recommendations);
        }

        return losses;
    }
}
