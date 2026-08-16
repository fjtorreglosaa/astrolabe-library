using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Billing.Events;

/// <summary>A late return was priced. Carries the amount so notifications need not recompute it. Carries identifiers and values only.</summary>
public sealed record FineAssessed(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid FineId, Guid MemberId, Guid ReservationId, int DaysLate, Money Amount) : IDomainEvent;
