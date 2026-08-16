using Astrolabe.Application.Abstractions.Recommendations;
using Astrolabe.Application.Contracts.Recommendations;
using Astrolabe.Domain.Features.Catalog.Enums;
using Astrolabe.Domain.Features.Reservations.Enums;
using Astrolabe.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Astrolabe.Infrastructure.Features.Recommendations;

/// <summary>
/// Builds the anonymised payload a provider is given. BR-REC-005 lives here and nowhere else.
///
/// <para>
/// Read the projections below and note what is absent: no member identifier, no name, no email, no
/// reservation row, no date. The member's history is reduced to genre names and title strings before
/// it leaves this class, and a provider client cannot include what it is never handed.
/// </para>
/// </summary>
public sealed class ReadingProfileBuilder(AstrolabeDbContext context) : IReadingProfileBuilder
{
    private const int RecentTitleCount = 8;
    private const int CandidateCount = 40;

    public async Task<ReadingProfile> BuildAsync(
        Guid memberId, CancellationToken cancellationToken = default)
    {
        // Titles and genres only, and only for books this member actually borrowed. The join stays
        // in the database so no reservation row is ever materialised here.
        var history = await context.Reservations
            .AsNoTracking()
            .Where(reservation => reservation.MemberId == memberId
                && reservation.Status != ReservationStatus.Cancelled)
            .OrderByDescending(reservation => reservation.ConfirmedAt)
            .Join(context.Books, reservation => reservation.BookId, book => book.Id,
                (reservation, book) => new { book.Title, book.Genre })
            .Take(RecentTitleCount * 3)
            .ToListAsync(cancellationToken);

        var genres = history
            .GroupBy(entry => entry.Genre)
            .OrderByDescending(group => group.Count())
            .Select(group => group.Key.ToString())
            .ToList();

        var titles = history
            .Select(entry => entry.Title)
            .Distinct()
            .Take(RecentTitleCount)
            .ToList();

        // BR-REC-009 enforced by construction: the model is only ever offered books that have a copy
        // somewhere, so it cannot suggest something nobody can borrow. Filtering the answer
        // afterwards would work too, and would leave a model free to spend its output on titles that
        // get thrown away.
        var candidates = await context.Books
            .AsNoTracking()
            .Where(book => book.Status == BookStatus.Catalog
                && book.Copies.Any(copy => copy.TotalCount > 0))
            .OrderByDescending(book => book.Copies.Sum(copy => copy.TotalCount))
            .Take(CandidateCount)
            .Select(book => new CandidateBook(
                book.Id, book.Title, book.Author, book.Genre.ToString()))
            .ToListAsync(cancellationToken);

        return new ReadingProfile(genres, titles, candidates);
    }
}
