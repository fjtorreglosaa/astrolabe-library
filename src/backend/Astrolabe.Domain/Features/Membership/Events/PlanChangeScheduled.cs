using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Membership.Enums;

namespace Astrolabe.Domain.Features.Membership.Events;

/// <summary>A downgrade was requested and will land at the renewal date. Carries identifiers and values only.</summary>
public sealed record PlanChangeScheduled(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid MemberId, PlanTier From, PlanTier Target, DateTimeOffset EffectiveOn) : IDomainEvent;
