using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Membership;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Abstractions.Network;
using Astrolabe.Application.Contracts.Reservations;
using Astrolabe.Application.Shared.Reservations;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Abstractions.Persistence;
using Astrolabe.Domain.Features.Audit.Entities;
using Astrolabe.Domain.Features.Audit.Repositories;
using Astrolabe.Domain.Features.Catalog.Errors;
using Astrolabe.Domain.Features.Catalog.Policies;
using Astrolabe.Domain.Features.Catalog.ValueObjects;
using Astrolabe.Domain.Features.Reservations.Entities;
using Astrolabe.Domain.Features.Reservations.Errors;
using Astrolabe.Domain.Features.Reservations.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Reservations.Commands.ConfirmReservation;

/// <summary>
/// Takes a copy off a shelf. The highest-risk unit in Stage 3.
///
/// <para>
/// Two members reaching for the last copy is the one race this product actually has, and BR-RSV-006
/// admits no exception. The protection is the concurrency token on the copy row, not the in-memory
/// guard: <c>Take()</c> only keeps the aggregate honest for whichever caller wins the commit.
/// </para>
/// </summary>
public sealed class ConfirmReservationCommandHandler(
    IReservationUnitOfWork reservations,
    IAuditUnitOfWork audit,
    IEntitlementProvider entitlements,
    ILibraryLocationProvider libraries,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : ICommandHandler<ConfirmReservationCommand, ReservationDto>
{
    public async Task<Result<ReservationDto>> Handle(
        ConfirmReservationCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } memberId)
        {
            return Result.Failure<ReservationDto>(ReservationErrors.NotYours);
        }

        // BR-RSV-008. A retried confirmation on a flaky connection is the common case; without the
        // key it takes a second copy. Checked first, so a replay never even reads the shelf.
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existing = await reservations.Reservations.GetByIdempotencyKeyAsync(
                memberId, request.IdempotencyKey.Trim(), cancellationToken);

            if (existing is not null)
            {
                return await DescribeAsync(existing, cancellationToken);
            }
        }

        var book = await reservations.Books.GetWithCopiesAsync(request.BookId, cancellationToken);

        if (book is null || !book.IsVisibleToMembers)
        {
            return Result.Failure<ReservationDto>(CatalogErrors.BookNotFound);
        }

        var copy = book.CopyAt(request.LibraryId);

        if (copy is null)
        {
            return Result.Failure<ReservationDto>(ReservationErrors.NoCopyAtLibrary);
        }

        // BR-RSV-007. Two active reservations of one physical copy make no sense, and the member
        // would have no way to tell the two returns apart.
        if (await reservations.Reservations.HasActiveForCopyAsync(memberId, copy.Id, cancellationToken))
        {
            return Result.Failure<ReservationDto>(ReservationErrors.AlreadyReserved);
        }

        // BR-RSV-004. catalog owns this decision; asking it here is what keeps one rule in one place.
        var member = await entitlements.GetForCurrentMemberAsync(cancellationToken);
        var locations = await libraries.GetAllAsync(cancellationToken);
        var location = locations.GetValueOrDefault(request.LibraryId);

        // BR-NET-005. Checked here and not only in the catalogue projection: hiding a branch from
        // the listing is presentation, and a member holding a stale page — or anyone posting the
        // identifier directly — would otherwise still reserve from a library that has been
        // withdrawn. This was reachable before NET-025 closed it.
        if (location is { IsActive: false })
        {
            return Result.Failure<ReservationDto>(ReservationErrors.LibraryInactive);
        }

        var verdict = CatalogAccessPolicy.EvaluateCopy(
            member, book.Tier,
            new CopyLocation(copy.LibraryId, location?.CityId ?? Guid.Empty, copy.AvailableCount));

        if (!verdict.CanReserve)
        {
            // The member's own home library, not the branch they asked for: naming the requested one
            // would read "Basic borrows at Loop only" while refusing them at Loop.
            var homeLibraryName = member.HomeLibraryId is { } homeId
                ? locations.GetValueOrDefault(homeId)?.LibraryName
                : null;

            return Result.Failure<ReservationDto>(
                ReservationAccess.ToError(verdict.Reason, homeLibraryName, location?.CityName));
        }

        // BR-RSV-005. The in-memory guard; the row's token is what actually decides the race.
        var taken = copy.Take();

        if (taken.IsFailure)
        {
            return Result.Failure<ReservationDto>(ReservationErrors.CopyJustTaken);
        }

        var now = clock.UtcNow;

        var reservation = Reservation.Confirm(
            memberId, book.Id, copy.Id, request.LibraryId,
            request.Delivery, request.IdempotencyKey, now);

        await reservations.Reservations.AddAsync(reservation, cancellationToken);

        await audit.Entries.AddAsync(
            AuditEntry.Record(
                "reservations.confirmed", now,
                actorUserId: memberId, subjectUserId: memberId,
                detail: $"{book.Title} · due {reservation.Period.DueOn:yyyy-MM-dd}"),
            cancellationToken);

        try
        {
            // One commit for the copy, the reservation and the audit entry. They share a change
            // tracker, so a crash cannot leave a reservation for a copy still on the shelf.
            await reservations.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            // Somebody else moved the copy row between the read and the commit. The member saw it
            // available a second ago, so they are told it has just gone — not that it never existed.
            return Result.Failure<ReservationDto>(ReservationErrors.CopyJustTaken);
        }

        return await DescribeAsync(reservation, cancellationToken);
    }

    private async Task<Result<ReservationDto>> DescribeAsync(
        Reservation reservation, CancellationToken cancellationToken)
    {
        var book = await reservations.Books.GetByIdAsync(reservation.BookId, cancellationToken);
        var locations = await libraries.GetAllAsync(cancellationToken);

        return Result.Success(ReservationProjection.ToDto(reservation, book, locations, clock.UtcNow));
    }
}
