using Astrolabe.Domain.Abstractions;

namespace Astrolabe.Domain.Features.Catalog.Events;

/// <summary>A review was written or rewritten. Triggers the book's rating to be recomputed.</summary>
public sealed record ReviewPublished(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid BookId,
    Guid MemberId) : IDomainEvent;
