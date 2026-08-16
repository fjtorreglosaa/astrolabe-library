using Astrolabe.Domain.Abstractions;

namespace Astrolabe.Domain.Features.Reservations.Events;

/// <summary>A copy left the shelf. Carries identifiers and values only.</summary>
public sealed record ReservationConfirmed(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid ReservationId,
    Guid MemberId,
    Guid BookId,
    Guid BookCopyId,
    Guid LibraryId,
    DateTimeOffset DueOn) : IDomainEvent;
