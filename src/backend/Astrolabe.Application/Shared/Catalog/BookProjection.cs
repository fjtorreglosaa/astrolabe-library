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
    public sealed record LibraryLocation(Guid LibraryId, string LibraryName, Guid CityId, string CityName);

    public static BookSummaryDto ToSummary(
        Book book,
        MemberEntitlement member,
        IReadOnlyDictionary<Guid, LibraryLocation> libraries)
    {
        var verdict = CatalogAccessPolicy.EvaluateBook(member, book.Tier, ToLocations(book, libraries));

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
            book.Copies.Sum(copy => copy.AvailableCount),
            book.Copies.Sum(copy => copy.TotalCount),
            verdict.CanReserve,
            verdict.Badge?.ToString());
    }

    public static BookDetailDto ToDetail(
        Book book,
        MemberEntitlement member,
        IReadOnlyDictionary<Guid, LibraryLocation> libraries)
    {
        var verdict = CatalogAccessPolicy.EvaluateBook(member, book.Tier, ToLocations(book, libraries));

        var copies = book.Copies.Select(copy =>
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
        Book book, IReadOnlyDictionary<Guid, LibraryLocation> libraries) =>
        book.Copies
            .Select(copy => new CopyLocation(
                copy.LibraryId,
                libraries.GetValueOrDefault(copy.LibraryId)?.CityId ?? Guid.Empty,
                copy.AvailableCount))
            .ToList();
}
