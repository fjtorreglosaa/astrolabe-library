using Astrolabe.Application.Abstractions.Catalog;
using Astrolabe.Domain.Features.Reservations.Enums;
using Astrolabe.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Astrolabe.Infrastructure.Features.Catalog;

/// <summary>
/// Reads the reservations table to answer whether a member has finished with a book.
/// </summary>
/// <remarks>
/// <c>AnyAsync</c> rather than a count: the question is whether one exists, and the provider can
/// stop at the first row it finds.
/// </remarks>
public sealed class BorrowingHistoryProbe(AstrolabeDbContext context) : IBorrowingHistoryProbe
{
    public Task<bool> HasReturnedAsync(
        Guid memberId, Guid bookId, CancellationToken cancellationToken = default) =>
        context.Reservations
            .AsNoTracking()
            .AnyAsync(
                reservation => reservation.MemberId == memberId
                    && reservation.BookId == bookId
                    && reservation.Status == ReservationStatus.Returned,
                cancellationToken);
}
