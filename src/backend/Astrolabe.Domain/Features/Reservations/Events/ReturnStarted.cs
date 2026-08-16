using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Reservations.Enums;

namespace Astrolabe.Domain.Features.Reservations.Events;

/// <summary>
/// The member handed the copy over and proved it with the handover code. The copy is not back on the
/// shelf — only a check-in does that.
/// </summary>
public sealed record ReturnStarted(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid ReservationId,
    Guid MemberId,
    Guid BookCopyId,
    ReturnMethod Method) : IDomainEvent;
