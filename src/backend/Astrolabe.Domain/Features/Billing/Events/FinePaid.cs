using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Billing.Events;

/// <summary>A fine settled, by card or at a desk. Carries identifiers and values only.</summary>
public sealed record FinePaid(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid FineId, Guid MemberId, Money Amount) : IDomainEvent;
