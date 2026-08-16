using Astrolabe.Application.Contracts.Catalog;
using Astrolabe.Domain.Features.Catalog.Entities;
using Astrolabe.Domain.Features.Catalog.Policies;
using Astrolabe.Domain.Features.Catalog.ValueObjects;
using Astrolabe.Domain.Features.Membership.ValueObjects;

namespace Astrolabe.Application.Shared.Catalog;

/// <summary>
/// Turns a book and a member's entitlement into the shapes the interface renders.
///
/// <para>
/// Sits in <c>Shared</c> rather than inside a feature folder because three queries need it — the
/// listing, the detail panel and, later, the store — and duplicating the access call in each would
/// give three chances to forget it. It holds no state and reaches no repository: it is a projection,
/// not a service.
/// </para>
/// </summary>
public static class BookProjection
{
    /// <summary>
    /// The geography a copy needs, keyed by library. Supplied by the caller because resolving it
    /// belongs to <c>network</c>, and looking it up per book would be an N+1 across a page.
    /// </summary>
    /// <param name="IsActive">
    /// Whether the branch is still open to members. BR-NET-005 says a deactivated library is hidden
    /// from them while its history is preserved, and this projection is where "hidden" is enforced
    /// for the catalogue.
    /// </param>
    public sealed record LibraryLocation(
        Guid LibraryId, string LibraryName, Guid CityId, string CityName, bool IsActive);

    /// <summary>
    /// The copies a member is allowed to see: those at branches still open to them.
    ///
    /// <para>
    /// A copy at a withdrawn branch is dropped rather than shown as unreservable. BR-NET-005 asks
    /// for the branch to be <em>hidden</em>, and listing it with a refusal would advertise a library
    /// members can no longer use. Staff projections do not go through here and still see everything.
    /// </para>
    /// <para>
    /// A copy whose library is unknown is kept. That is a data fault, not a withdrawal, and hiding
    /// it would make a book look emptier than it is; <see cref="ToLocations"/> gives it an empty
    /// city so it can never be judged reachable.
    /// </para>
    /// </summary>
    private static List<BookCopy> VisibleCopies(
        Book book, IReadOnlyDictionary<Guid, LibraryLocation> libraries) =>
        book.Copies
            .Where(copy => libraries.GetValueOrDefault(copy.LibraryId) is not { IsActive: false })
            .ToList();

    public static BookSummaryDto ToSummary(
        Book book,
        MemberEntitlement member,
        IReadOnlyDictionary<Guid, LibraryLocation> libraries)
    {
        var visible = VisibleCopies(book, libraries);
        var verdict = CatalogAccessPolicy.EvaluateBook(member, book.Tier, ToLocations(visible, libraries));

        return new BookSummaryDto(
            book.Id,
            book.Isbn.Value,
            book.Title,
            book.Author,
            book.Genre.ToString(),
            book.Tier.ToString(),
            (int)book.RetailPrice.Cents,
            book.CoverUrl,
            book.AverageRating,
            book.ReviewCount,
            visible.Sum(copy => copy.AvailableCount),
            visible.Sum(copy => copy.TotalCount),
            verdict.CanReserve,
            verdict.Badge?.ToString());
    }

    public static BookDetailDto ToDetail(
        Book book,
        MemberEntitlement member,
        IReadOnlyDictionary<Guid, LibraryLocation> libraries)
    {
        var visible = VisibleCopies(book, libraries);
        var verdict = CatalogAccessPolicy.EvaluateBook(member, book.Tier, ToLocations(visible, libraries));

        var copies = visible.Select(copy =>
        {
            var location = libraries.GetValueOrDefault(copy.LibraryId);
            var copyVerdict = verdict.Copies.FirstOrDefault(v => v.LibraryId == copy.LibraryId);

            return new CopyAvailabilityDto(
                copy.LibraryId,
                location?.LibraryName ?? "Unknown library",
                location?.CityName ?? "Unknown city",
                copy.AvailableCount,
                copy.TotalCount,
                copyVerdict?.CanReserve ?? false,
                copyVerdict?.Reason?.ToString());
        }).ToList();

        return new BookDetailDto(
            book.Id,
            book.Isbn.Value,
            book.Title,
            book.Author,
            book.Publisher,
            book.Genre.ToString(),
            book.Tier.ToString(),
            (int)book.RetailPrice.Cents,
            book.CoverUrl,
            book.AverageRating,
            book.ReviewCount,
            verdict.CanReserve,
            verdict.Badge?.ToString(),
            copies);
    }

    public static StaffBookDto ToStaffRow(Book book) =>
        new(book.Id,
            book.Isbn.Value,
            book.Title,
            book.Author,
            book.Genre.ToString(),
            book.Tier.ToString(),
            book.Status.ToString(),
            (int)book.RetailPrice.Cents,
            book.Copies.Sum(copy => copy.AvailableCount),
            book.Copies.Sum(copy => copy.TotalCount),
            book.CreatedAt);

    /// <summary>
    /// A copy whose library is unknown is given an empty city, so it can never accidentally match a
    /// member's city and be treated as reachable. Silently dropping it would instead make a book
    /// look emptier than it is.
    /// </summary>
    private static List<CopyLocation> ToLocations(
        IEnumerable<BookCopy> copies, IReadOnlyDictionary<Guid, LibraryLocation> libraries) =>
        copies
            .Select(copy => new CopyLocation(
                copy.LibraryId,
                libraries.GetValueOrDefault(copy.LibraryId)?.CityId ?? Guid.Empty,
                copy.AvailableCount))
            .ToList();
}
