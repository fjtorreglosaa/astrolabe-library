namespace Astrolabe.Domain.Features.Membership.Enums;

/// <summary>
/// A subscription tier, and equally the tier a book requires.
///
/// The values are ordered so "is this book within this member's plan" is a comparison rather than a
/// lookup table. Shared between <c>membership</c> and <c>catalog</c> precisely because the whole
/// access rule turns on comparing the two.
/// </summary>
public enum PlanTier
{
    Basic = 0,
    Plus = 1,
    Max = 2
}

public static class PlanTierExtensions
{
    /// <summary>True when a book of <paramref name="bookTier"/> is within <paramref name="plan"/>.</summary>
    public static bool Covers(this PlanTier plan, PlanTier bookTier) => plan >= bookTier;

    public static bool IsHigherThan(this PlanTier plan, PlanTier other) => plan > other;

    public static bool IsPaid(this PlanTier plan) => plan is not PlanTier.Basic;
}
