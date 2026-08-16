using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Billing.Entities;
using Astrolabe.Domain.Features.Billing.Repositories;
using Astrolabe.Domain.Features.Catalog.Repositories;
using Astrolabe.Domain.Features.Reservations.Enums;
using Astrolabe.Domain.Features.Reservations.Errors;
using Astrolabe.Domain.Features.Reservations.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Billing.Commands.AssessFine;

public sealed class AssessFineCommandHandler(
    IBillingUnitOfWork billing,
    IReservationRepository reservations,
    IBookRepository books,
    IDateTimeProvider clock) : ICommandHandler<AssessFineCommand, Guid?>
{
    public async Task<Result<Guid?>> Handle(
        AssessFineCommand request, CancellationToken cancellationToken)
    {
        // BR-BIL-010, checked first: the common case is the job re-visiting a reservation the event
        // handler already priced, and it must cost one query and stop.
        var existing = await billing.Fines.GetByReservationAsync(request.ReservationId, cancellationToken);

        if (existing is not null)
        {
            return Result.Success<Guid?>(existing.Id);
        }

        var reservation = await reservations.GetByIdAsync(request.ReservationId, cancellationToken);

        if (reservation is null)
        {
            return Result.Failure<Guid?>(ReservationErrors.NotFound);
        }

        // Only a completed loan can be priced. A copy still out has no final lateness, and pricing
        // one would produce a fine that BR-BIL-003 says must never grow afterwards.
        if (reservation.Status is not ReservationStatus.Returned)
        {
            return Result.Success<Guid?>(null);
        }

        var book = await books.GetByIdAsync(reservation.BookId, cancellationToken);

        // Priced from the days frozen at check-in, never recomputed from the clock.
        var fine = Fine.Assess(
            reservation.MemberId,
            reservation.Id,
            reservation.LibraryId,
            book?.Title ?? "Unknown title",
            reservation.DaysLateAtCheckIn,
            clock.UtcNow);

        // BR-BIL-009: an on-time return produces no fine at all, not a fine of zero.
        if (fine is null)
        {
            return Result.Success<Guid?>(null);
        }

        await billing.Fines.AddAsync(fine, cancellationToken);

        // The charge and the fine commit together: a fine with no ledger entry is a debt the
        // statement does not show.
        await billing.Ledger.AddAsync(
            LedgerEntry.Charge(
                fine.MemberId, fine.Amount,
                $"Late fine — {fine.BookTitle}", fine.Id, reservation.Id, clock.UtcNow),
            cancellationToken);

        await billing.SaveChangesAsync(cancellationToken);

        return Result.Success<Guid?>(fine.Id);
    }
}
