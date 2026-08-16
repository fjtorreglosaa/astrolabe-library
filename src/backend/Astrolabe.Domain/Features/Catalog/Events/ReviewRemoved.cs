using Astrolabe.Domain.Abstractions;

namespace Astrolabe.Domain.Features.Catalog.Events;

/// <summary>A review was withdrawn. BR-CAT-031 requires the rating to be recomputed immediately.</summary>
public sealed record ReviewRemoved(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid BookId,
    Guid MemberId) : IDomainEvent;
