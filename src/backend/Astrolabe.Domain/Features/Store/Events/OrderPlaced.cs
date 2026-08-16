using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Store.Events;

/// <summary>
/// An order was paid. Carries the total and the points so a consumer need not reprice it.
/// Carries identifiers and values only.
/// </summary>
public sealed record OrderPlaced(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid OrderId,
    Guid MemberId,
    Money Total,
    int PointsEarned,
    int LineCount) : IDomainEvent;
