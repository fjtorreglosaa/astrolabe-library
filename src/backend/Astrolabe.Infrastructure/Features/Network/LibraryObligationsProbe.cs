using Astrolabe.Application.Abstractions.Network;
using Astrolabe.Application.Contracts.Network;
using Astrolabe.Domain.Features.Billing.Enums;
using Astrolabe.Domain.Features.Reservations.Enums;
using Astrolabe.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Astrolabe.Infrastructure.Features.Network;

/// <summary>
/// Counts what a library still holds, across the three domains that own the facts.
///
/// <para>
/// This replaces the placeholder that answered "none" to everything (<c>NET-025</c>). Three separate
/// counts rather than one join: the tables share no key beyond the library, so a join would multiply
/// rows and each count would have to be made distinct again to be correct.
/// </para>
/// </summary>
public sealed class LibraryObligationsProbe(AstrolabeDbContext context) : ILibraryObligationsProbe
{
    public async Task<LibraryObligations> GetAsync(
        Guid libraryId, CancellationToken cancellationToken = default)
    {
        // Volumes, not rows: a single row records how many copies of one title a library holds, so
        // counting rows would report "3 books" where the shelves carry thirty.
        var copies = await context.BookCopies
            .AsNoTracking()
            .Where(copy => copy.LibraryId == libraryId)
            .SumAsync(copy => copy.TotalCount, cancellationToken);

        var reservations = await context.Reservations
            .AsNoTracking()
            .CountAsync(
                reservation => reservation.LibraryId == libraryId
                    && (reservation.Status == ReservationStatus.Reserved
                        || reservation.Status == ReservationStatus.InTransit),
                cancellationToken);

        // AwaitingValidation counts as unresolved: the member has paid at a desk but no
        // administrator has confirmed it, so the money is not yet settled.
        var fines = await context.Fines
            .AsNoTracking()
            .CountAsync(
                fine => fine.LibraryId == libraryId
                    && (fine.Status == FineStatus.Outstanding
                        || fine.Status == FineStatus.AwaitingValidation),
                cancellationToken);

        return new LibraryObligations(copies, reservations, fines);
    }
}
