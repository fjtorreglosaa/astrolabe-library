using Astrolabe.Domain.Abstractions;

namespace Astrolabe.Domain.Features.Catalog.Events;

/// <summary>A book returned to the catalogue, from repair or from removal.</summary>
public sealed record BookRestored(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid BookId,
    string Title) : IDomainEvent;
