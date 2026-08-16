using Astrolabe.Domain.Features.Catalog.ValueObjects;
using Astrolabe.Domain.Features.Membership.Enums;
using Astrolabe.Domain.Features.Membership.ValueObjects;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Store.Policies;

/// <summary>
/// What a member pays for a book. Implements BR-STR-001 to BR-STR-003 and BR-STR-009.
///
/// <para>
/// A <b>pure static function</b> over the entitlement and where the book is held, like
/// <c>CatalogAccessPolicy</c> and <c>FinePolicy</c>: no repository, no clock, so the whole table can
/// be exercised as fast unit tests.
/// </para>
/// <para>
/// It is deliberately <b>not</b> <c>MemberEntitlement.DiscountPercent</c>. That field says what a
/// plan is worth in general; <c>BR-STR-002</c> makes a Plus member's discount depend on whether a
/// library in their city holds the book. Reading the entitlement's percentage directly is right for
/// Basic and Max and silently wrong for Plus — which is the sort of error that shows up as a member
/// being overcharged and nobody knowing why.
/// </para>
/// </summary>
public static class PurchaseDiscountPolicy
{
    /// <summary>
    /// The percentage this member earns on this book.
    ///
    /// Reach is asked of the copies rather than of the access rule: <c>BR-STR-012</c> lets anyone buy
    /// anything, so this is only ever about the discount and never about the right to purchase.
    /// </summary>
    public static int PercentFor(MemberEntitlement member, IReadOnlyList<CopyLocation> copies) =>
        member.Reach switch
        {
            // BR-STR-003. Held by any library at all is enough.
            ReachKind.Network => copies.Count > 0 ? 15 : 0,

            // BR-STR-002. One copy in the member's city satisfies it; the rest are irrelevant.
            ReachKind.City => copies.Any(copy => copy.CityId == member.CityId) ? 10 : 0,

            // BR-STR-001. Basic earns nothing, wherever the book sits.
            _ => 0
        };

    /// <summary>
    /// The discount on one line, rounded to the nearest cent and clamped to the price.
    ///
    /// Rounded here rather than on the order total: <c>BR-STR-004</c>. The two disagree by a cent
    /// often enough that a receipt stops adding up, and a receipt that does not add up produces a
    /// complaint nobody can reproduce.
    /// </summary>
    public static Money DiscountOn(Money linePrice, int percent)
    {
        if (percent <= 0 || linePrice.Cents <= 0)
        {
            return Money.Zero;
        }

        var discounted = (long)Math.Round(
            linePrice.Cents * percent / 100d, MidpointRounding.AwayFromZero);

        // BR-STR-009: a discount can never exceed the price. Impossible at 10 or 15 percent, and
        // guarded anyway so a future percentage cannot make a line negative.
        return Money.FromCents(Math.Min(discounted, linePrice.Cents));
    }
}
