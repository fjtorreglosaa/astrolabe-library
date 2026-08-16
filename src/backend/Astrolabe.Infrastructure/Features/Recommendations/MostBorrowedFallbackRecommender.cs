using Astrolabe.Application.Abstractions.Recommendations;
using Astrolabe.Application.Contracts.Recommendations;
using Astrolabe.Application.Shared.Recommendations;
using Astrolabe.Domain.Features.Catalog.Enums;
using Astrolabe.Domain.Features.Reservations.Enums;
using Astrolabe.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Astrolabe.Infrastructure.Features.Recommendations;

/// <summary>
/// The most-borrowed ranking. Backs BR-REC-003 and the last resort of BR-REC-007.
///
/// <para>
/// A plain catalogue query, and that is the requirement rather than an implementation detail: this
/// is where every other path goes when it fails, so it must not depend on anything that can fail
/// with it. No provider, no credential, no cache.
/// </para>
/// <para>
/// Narrowed to the member's own genres when they have any, which is what the prototype promises —
/// "the most borrowed titles in your genres" — and widened to the whole catalogue when they do not,
/// because a new member with no history still deserves a list.
/// </para>
/// </summary>
public sealed class MostBorrowedFallbackRecommender(AstrolabeDbContext context)
    : IFallbackRecommender
{
    public async Task<IReadOnlyList<ProviderSuggestion>> GetAsync(
        Guid memberId, int count, CancellationToken cancellationToken = default)
    {
        var genres = await context.Reservations
            .AsNoTracking()
            .Where(reservation => reservation.MemberId == memberId)
            .Join(context.Books, reservation => reservation.BookId, book => book.Id,
                (_, book) => book.Genre)
            .Distinct()
            .ToListAsync(cancellationToken);

        // Already borrowed by this member: a recommendation to read what you have just read is not
        // one. Excluded here rather than after ranking, so the list still comes back full.
        var alreadyRead = await context.Reservations
            .AsNoTracking()
            .Where(reservation => reservation.MemberId == memberId)
            .Select(reservation => reservation.BookId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var query = context.Books
            .AsNoTracking()
            .Where(book => book.Status == BookStatus.Catalog
                && !alreadyRead.Contains(book.Id)
                && book.Copies.Any(copy => copy.TotalCount > 0));

        if (genres.Count > 0)
        {
            query = query.Where(book => genres.Contains(book.Genre));
        }

        var ranked = await query
            .Select(book => new
            {
                book.Id,
                Borrows = context.Reservations.Count(reservation =>
                    reservation.BookId == book.Id
                    && reservation.Status != ReservationStatus.Cancelled),
            })
            .OrderByDescending(row => row.Borrows)
            // Id last, so two equally borrowed books cannot swap places between calls and make the
            // list look like it changed when nothing did.
            .ThenBy(row => row.Id)
            .Take(count)
            .ToListAsync(cancellationToken);

        // BR-REC-010 exempts nothing, so the fallback states its reason too. It is a weaker reason
        // than a model's, and saying it plainly is better than leaving a blank line.
        return [.. ranked.Select(row => new ProviderSuggestion(
            row.Id, RecommendationCopy.FallbackReason, MatchPercent: 0))];
    }
}
