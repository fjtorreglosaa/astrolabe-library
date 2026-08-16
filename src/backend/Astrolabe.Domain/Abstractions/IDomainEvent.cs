namespace Astrolabe.Domain.Abstractions;

/// <summary>
/// A fact about something that has happened in the domain. Named in the past tense, immutable, and
/// carrying only identifiers and values — never entity references, which would let a handler mutate
/// state the event has already described.
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }

    DateTimeOffset OccurredAt { get; }
}
