using Astrolabe.Domain.Features.Membership.Enums;

namespace Astrolabe.Domain.Features.Recommendations.Policies;

/// <summary>
/// Who sees what on the recommendations surface. Implements BR-REC-002 and BR-REC-003.
///
/// <para>
/// A pure decision over a plan and a count, so every branch is testable without a database and no
/// handler can reach the answer another way. The same shape as <c>CatalogAccessPolicy</c>, and for
/// the same reason: an access rule spread across handlers is an access rule with exceptions.
/// </para>
/// </summary>
public static class RecommendationAccessPolicy
{
    /// <summary>
    /// BR-REC-002. Basic never sees the surface at all — not an empty one, not a fallback one.
    ///
    /// The distinction matters: a Basic member is not a member whose library happens to be
    /// unconnected, and showing them the most-borrowed list would quietly hand them a benefit their
    /// plan excludes while telling them nothing about why the real thing is missing.
    /// </summary>
    public static bool PlanIncludesRecommendations(PlanTier plan) =>
        plan is PlanTier.Plus or PlanTier.Max;

    /// <summary>
    /// BR-REC-003. Whether a member gets a model answer or the most-borrowed fallback.
    ///
    /// Decided by whether <b>any</b> library in their city is connected, which is what the prototype
    /// does. Not their home library alone: a member should not lose recommendations because the
    /// branch nearest them has not paid for a key while the branch across town has.
    /// </summary>
    public static RecommendationVerdict Evaluate(PlanTier plan, int connectedLibrariesInCity)
    {
        if (!PlanIncludesRecommendations(plan))
        {
            return RecommendationVerdict.NotIncludedInPlan;
        }

        return connectedLibrariesInCity > 0
            ? RecommendationVerdict.ModelGenerated
            : RecommendationVerdict.Fallback;
    }
}
