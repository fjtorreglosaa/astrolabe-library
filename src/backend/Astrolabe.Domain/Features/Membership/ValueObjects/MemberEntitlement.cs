using Astrolabe.Domain.Features.Membership.Enums;

namespace Astrolabe.Domain.Features.Membership.ValueObjects;

/// <summary>
/// What a member is entitled to, as every other domain consumes it.
///
/// <para>
/// Published as one record so the plan table lives in a single place. Without it, <c>catalog</c>,
/// <c>store</c> and <c>recommendations</c> would each carry a switch over the plan, and the day a
/// price or a percentage changes, three of them would be updated and one forgotten.
/// </para>
/// </summary>
public sealed record MemberEntitlement
{
    /// <summary>What an anonymous or unknown caller gets: nothing, everywhere.</summary>
    public static readonly MemberEntitlement None = new()
    {
        Plan = PlanTier.Basic,
        Reach = ReachKind.HomeLibraryOnly,
        CityId = null,
        HomeLibraryId = null,
        DiscountPercent = 0,
        EarnsPoints = false,
        SeesRecommendations = false,
    };

    public required PlanTier Plan { get; init; }

    public required ReachKind Reach { get; init; }

    /// <summary>The member's city of residence. Null when unknown or when the plan is network-wide.</summary>
    public required Guid? CityId { get; init; }

    /// <summary>The only library a Basic member may borrow from.</summary>
    public required Guid? HomeLibraryId { get; init; }

    /// <summary>Whole percent off a purchase. Whether it applies to a given book is decided by reach.</summary>
    public required int DiscountPercent { get; init; }

    public required bool EarnsPoints { get; init; }

    public required bool SeesRecommendations { get; init; }

    /// <summary>True when a book of this tier is within the member's plan (BR-CAT-007 to BR-CAT-009).</summary>
    public bool CoversTier(PlanTier bookTier) => Plan.Covers(bookTier);
}
