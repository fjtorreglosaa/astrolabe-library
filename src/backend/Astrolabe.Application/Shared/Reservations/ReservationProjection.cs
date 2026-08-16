using Astrolabe.Application.Contracts.Reservations;
using Astrolabe.Application.Shared.Catalog;
using Astrolabe.Domain.Features.Catalog.Entities;
using Astrolabe.Domain.Features.Reservations.Entities;

namespace Astrolabe.Application.Shared.Reservations;

/// <summary>
/// Turns a reservation into the shape the interface renders.
///
/// <para>
/// Lateness is computed here, server-side, and travels as a value. The client must not derive it: a
/// browser clock that is wrong, or simply in another zone, would show a member a different lateness
/// than the desk sees — and lateness is what a fine is built on.
/// </para>
/// </summary>
public static class ReservationProjection
{
    public static ReservationDto ToDto(
        Reservation reservation,
        Book? book,
        IReadOnlyDictionary<Guid, BookProjection.LibraryLocation> libraries,
        DateTimeOffset now)
    {
        var location = libraries.GetValueOrDefault(reservation.LibraryId);

        return new ReservationDto(
            reservation.Id,
            reservation.BookId,
            // A book removed from the catalogue keeps its loans alive (BR-RSV-011), so a missing
            // title is a state to render rather than a reason to fail.
            book?.Title ?? "Unknown title",
            book?.Author ?? string.Empty,
            book?.CoverUrl,
            location?.LibraryName ?? "Unknown library",
            location?.CityName ?? string.Empty,
            reservation.Delivery.ToString(),
            (int)reservation.DeliveryFee.Cents,
            reservation.Period.StartedOn,
            reservation.Period.DueOn,
            reservation.Status.ToString(),
            reservation.IsOverdueAt(now),
            reservation.DaysLateAt(now),
            reservation.Period.DaysRemainingAt(now),
            reservation.ReturnMethod?.ToString(),
            reservation.HandedOverAt,
            reservation.CheckedInAt);
    }

    public static StaffReservationDto ToStaffDto(
        Reservation reservation,
        Book? book,
        string memberName,
        IReadOnlyDictionary<Guid, BookProjection.LibraryLocation> libraries,
        DateTimeOffset now) =>
        new(reservation.Id,
            memberName,
            book?.Title ?? "Unknown title",
            book?.Author ?? string.Empty,
            libraries.GetValueOrDefault(reservation.LibraryId)?.LibraryName ?? "Unknown library",
            reservation.Period.StartedOn,
            reservation.Period.DueOn,
            reservation.Status.ToString(),
            reservation.IsOverdueAt(now),
            reservation.DaysLateAt(now),
            reservation.ReturnMethod?.ToString());
}
