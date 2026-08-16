using Astrolabe.Domain.Abstractions;

namespace Astrolabe.Domain.Features.Reservations.Events;

/// <summary>
/// The library received the copy. The loan is over.
///
/// <para>
/// Carries <see cref="DaysLate"/> rather than a fine, per BR-RSV-020: <c>billing</c> owns the rate
/// and the cap, and carrying the number keeps this domain from having to know either. Stage 4
/// consumes this event.
/// </para>
/// </summary>
public sealed record ReservationReturned(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid ReservationId,
    Guid MemberId,
    Guid BookId,
    Guid BookCopyId,
    Guid LibraryId,
    int DaysLate) : IDomainEvent;
