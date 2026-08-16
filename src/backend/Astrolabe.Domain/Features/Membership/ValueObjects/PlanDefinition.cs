using Astrolabe.Domain.Features.Membership.Enums;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Membership.ValueObjects;

/// <summary>
/// What one plan costs and grants. Implements BR-MBR-002 to BR-MBR-009.
///
/// The three definitions live in <see cref="PlanCatalog"/> so the plan table exists exactly once.
/// </summary>
public sealed record PlanDefinition(
    PlanTier Tier,
    Money MonthlyPrice,
    ReachKind Reach,
    int DiscountPercent,
    bool EarnsPoints,
    bool SeesRecommendations);

/// <summary>
/// The three plans, with the prices and entitlements the prototype defines.
///
/// A static table rather than database rows: these are product decisions that change with a release,
/// not data an administrator edits. Putting them in the database would invite them to drift from the
/// copy on the pricing screen with nothing to catch it.
/// </summary>
public static class PlanCatalog
{
    public static readonly PlanDefinition Basic = new(
        PlanTier.Basic, Money.Zero, ReachKind.HomeLibraryOnly,
        DiscountPercent: 0, EarnsPoints: false, SeesRecommendations: false);

    public static readonly PlanDefinition Plus = new(
        PlanTier.Plus, Money.FromUnits(6, 99), ReachKind.City,
        DiscountPercent: 10, EarnsPoints: false, SeesRecommendations: true);

    public static readonly PlanDefinition Max = new(
        PlanTier.Max, Money.FromUnits(12, 99), ReachKind.Network,
        DiscountPercent: 15, EarnsPoints: true, SeesRecommendations: true);

    public static readonly IReadOnlyList<PlanDefinition> All = [Basic, Plus, Max];

    public static PlanDefinition For(PlanTier tier) => tier switch
    {
        PlanTier.Basic => Basic,
        PlanTier.Plus => Plus,
        _ => Max
    };

    /// <summary>Builds the entitlement other domains consume, from a plan and the member's geography.</summary>
    public static MemberEntitlement EntitlementFor(PlanTier tier, Guid? cityId, Guid? homeLibraryId)
    {
        var plan = For(tier);

        return new MemberEntitlement
        {
            Plan = plan.Tier,
            Reach = plan.Reach,
            CityId = cityId,
            HomeLibraryId = homeLibraryId,
            DiscountPercent = plan.DiscountPercent,
            EarnsPoints = plan.EarnsPoints,
            SeesRecommendations = plan.SeesRecommendations,
        };
    }
}
