using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Audit.Entities;
using Astrolabe.Domain.Features.Audit.Repositories;
using Astrolabe.Domain.Features.Reservations.Errors;
using Astrolabe.Domain.Features.Reservations.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Reservations.Commands.BeginReturn;

public sealed class BeginReturnCommandHandler(
    IReservationUnitOfWork reservations,
    IAuditUnitOfWork audit,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : ICommandHandler<BeginReturnCommand>
{
    public async Task<Result> Handle(BeginReturnCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } memberId)
        {
            return Result.Failure(ReservationErrors.NotYours);
        }

        var reservation = await reservations.Reservations.GetByIdAsync(
            request.ReservationId, cancellationToken);

        if (reservation is null)
        {
            return Result.Failure(ReservationErrors.NotFound);
        }

        var now = clock.UtcNow;

        // The entity checks ownership as well as the code. A member who guesses a code they heard
        // read aloud still cannot start somebody else's return.
        var result = reservation.BeginReturn(memberId, request.Method, request.Code, now);

        if (result.IsFailure)
        {
            return result;
        }

        // BR-RSV-023. Inside the transaction, never in an event handler: a reaction runs after the
        // commit and may be lost, and a trail that can skip a handover is not a trail.
        await audit.Entries.AddAsync(
            AuditEntry.Record(
                "reservations.return_started", now,
                actorUserId: memberId, subjectUserId: memberId,
                detail: request.Method.ToString()),
            cancellationToken);

        // Nothing goes back on the shelf here. Only the library receiving the copy does that.
        await reservations.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
