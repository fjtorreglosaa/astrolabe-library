using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Abstractions.Network;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Audit.Entities;
using Astrolabe.Domain.Features.Audit.Repositories;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Network.Errors;
using Astrolabe.Domain.Features.Reservations.Errors;
using Astrolabe.Domain.Features.Reservations.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Reservations.Commands.CheckInReservation;

public sealed class CheckInReservationCommandHandler(
    IReservationUnitOfWork reservations,
    IAuditUnitOfWork audit,
    ILibraryScopeProvider scope,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : ICommandHandler<CheckInReservationCommand>
{
    public async Task<Result> Handle(
        CheckInReservationCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.Role is not { } role || !role.IsStaff())
        {
            return Result.Failure(NetworkErrors.StaffRequired);
        }

        var reservation = await reservations.Reservations.GetByIdAsync(
            request.ReservationId, cancellationToken);

        if (reservation is null)
        {
            return Result.Failure(ReservationErrors.NotFound);
        }

        // BR-RSV-018. A librarian receives copies at their own desk, not at somebody else's.
        var reach = await scope.GetCurrentScopeAsync(cancellationToken);

        if (!reach.Covers(reservation.LibraryId))
        {
            return Result.Failure(ReservationErrors.LibraryOutOfScope);
        }

        var now = clock.UtcNow;
        var checkedIn = reservation.CheckIn(now);

        if (checkedIn.IsFailure)
        {
            return Result.Failure(checkedIn.Error);
        }

        // BR-RSV-019: false means it was already returned. Restoring stock again would invent a
        // volume the library does not own.
        if (!checkedIn.Value)
        {
            return Result.Success();
        }

        var book = await reservations.Books.GetWithCopiesAsync(reservation.BookId, cancellationToken);
        var copy = book?.Copies.FirstOrDefault(c => c.Id == reservation.BookCopyId);

        // BR-RSV-017. A copy row removed from under a live loan should not block the return; the
        // reservation still closes, and the shelf is simply not credited.
        copy?.Return();

        await audit.Entries.AddAsync(
            AuditEntry.Record(
                "reservations.checked_in", now,
                actorUserId: currentUser.UserId, subjectUserId: reservation.MemberId,
                detail: reservation.DaysLateAtCheckIn > 0
                    ? $"{reservation.DaysLateAtCheckIn} day(s) late"
                    : "on time"),
            cancellationToken);

        await reservations.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
