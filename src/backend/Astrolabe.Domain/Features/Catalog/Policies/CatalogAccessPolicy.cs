using Astrolabe.Domain.Features.Catalog.Enums;
using Astrolabe.Domain.Features.Catalog.ValueObjects;
using Astrolabe.Domain.Features.Membership.Enums;
using Astrolabe.Domain.Features.Membership.ValueObjects;

namespace Astrolabe.Domain.Features.Catalog.Policies;

/// <summary>
/// Whether a member may reserve a copy, and if not, why. Implements BR-CAT-006 to BR-CAT-016.
///
/// <para>
/// The single most consequential rule in the product: <c>reservations</c> refuses a loan on it,
/// <c>store</c> prices a purchase on it, and the interface explains itself with it. It is written
/// once here and consumed everywhere, so those three can never disagree.
/// </para>
/// <para>
/// A <b>pure static function</b> over value inputs: no repository, no clock, no database. That is
/// what lets the whole access matrix — 3 plans × 3 tiers × in and out of city × in and out of the
/// home library × with and without stock — be exercised as fast unit tests rather than as
/// integration tests nobody runs often enough.
/// </para>
/// <para>
/// Transcribed from the prototype's <c>copyState</c> and <c>bookAccess</c>, which are the authority.
/// </para>
/// </summary>
public static class CatalogAccessPolicy
{
    /// <summary>
    /// The verdict for one copy. Order matters and follows the prototype exactly: stock first, then
    /// tier, then location.
    /// </summary>
    public static CopyAccessVerdict EvaluateCopy(
        MemberEntitlement member, PlanTier bookTier, CopyLocation copy)
    {
        // A copy nobody can take is refused for that reason first, whatever the member's plan.
        // Telling a Basic member their plan is the problem when the shelf is empty would send them
        // to upgrade for nothing.
        if (!copy.HasStock)
        {
            return CopyAccessVerdict.Refused(copy.LibraryId, CopyRejection.OutOfStock);
        }

        return member.Reach switch
        {
            ReachKind.HomeLibraryOnly => EvaluateForHomeLibraryOnly(member, bookTier, copy),
            ReachKind.City => EvaluateForCity(member, copy),

            // BR-CAT-009: a network plan has no tier and no location restriction left to apply.
            _ => CopyAccessVerdict.Allowed(copy.LibraryId)
        };
    }

    /// <summary>
    /// The verdict for a whole book. Reservable when at least one copy is (BR-CAT-010), and
    /// otherwise carrying the single badge the card shows.
    /// </summary>
    public static BookAccessVerdict EvaluateBook(
        MemberEntitlement member, PlanTier bookTier, IReadOnlyList<CopyLocation> copies)
    {
        var verdicts = copies.Select(copy => EvaluateCopy(member, bookTier, copy)).ToList();

        if (verdicts.Any(verdict => verdict.CanReserve))
        {
            return new BookAccessVerdict(true, null, verdicts);
        }

        return new BookAccessVerdict(false, BadgeFor(member, bookTier, copies), verdicts);
    }

    private static CopyAccessVerdict EvaluateForHomeLibraryOnly(
        MemberEntitlement member, PlanTier bookTier, CopyLocation copy)
    {
        // BR-CAT-007: the tier check comes before the location check. A Basic member looking at a
        // Plus title must be told about the tier, because pointing at a library would imply that
        // another library would help.
        if (!member.CoversTier(bookTier))
        {
            return CopyAccessVerdict.Refused(copy.LibraryId, CopyRejection.NotInBasicCatalog);
        }

        if (member.HomeLibraryId != copy.LibraryId)
        {
            return CopyAccessVerdict.Refused(copy.LibraryId, CopyRejection.HomeLibraryOnly);
        }

        return CopyAccessVerdict.Allowed(copy.LibraryId);
    }

    /// <summary>
    /// BR-CAT-008. A city plan carries no tier restriction at all, so the only question is where the
    /// copy sits.
    /// </summary>
    private static CopyAccessVerdict EvaluateForCity(MemberEntitlement member, CopyLocation copy) =>
        member.CityId == copy.CityId
            ? CopyAccessVerdict.Allowed(copy.LibraryId)
            : CopyAccessVerdict.Refused(copy.LibraryId, CopyRejection.OutsideCity);

    /// <summary>
    /// The one reason shown on the card. Implements BR-CAT-011 to BR-CAT-014, in the prototype's own
    /// order of precedence.
    /// </summary>
    private static BookRejection BadgeFor(
        MemberEntitlement member, PlanTier bookTier, IReadOnlyList<CopyLocation> copies)
    {
        // BR-CAT-012: the tier badge outranks everything, including an empty shelf. A member whose
        // plan excludes the book gains nothing from knowing it is also out of stock.
        if (member.Reach is ReachKind.HomeLibraryOnly && !member.CoversTier(bookTier))
        {
            return BookRejection.NotInBasicPlan;
        }

        if (!copies.Any(copy => copy.HasStock))
        {
            return BookRejection.AllCopiesOut;
        }

        // Stock exists somewhere, so what stops the member is reach.
        return member.Reach switch
        {
            ReachKind.HomeLibraryOnly => BookRejection.HomeLibraryOnly,
            ReachKind.City => BookRejection.NotInCity,

            // Unreachable for a network plan, which refuses nothing that has stock. Kept so an
            // unforeseen combination degrades to a plain refusal rather than to a claim about a
            // city or a library that would be false.
            _ => BookRejection.Unavailable
        };
    }
}
