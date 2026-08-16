using Astrolabe.Domain.Abstractions;

namespace Astrolabe.Domain.Features.Support.Events;

/// <summary>
/// An agent replied. Implements the trigger half of BR-SUP-012.
///
/// A consequence, not a step. The reply already committed, and the member being told about it is a
/// reaction that must not be able to fail the answer.
/// </summary>
public sealed record TicketAnswered(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid TicketId,
    Guid MemberId,
    string Reference,
    string Subject) : IDomainEvent;
