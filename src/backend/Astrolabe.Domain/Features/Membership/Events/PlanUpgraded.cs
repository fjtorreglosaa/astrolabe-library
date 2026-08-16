using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Membership.Enums;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Membership.Events;

/// <summary>
/// An upgrade applied immediately. Carries the prorated amount so billing can record the charge
/// without recomputing it and risking a different answer.
/// </summary>
public sealed record PlanUpgraded(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid MemberId,
    PlanTier From,
    PlanTier To,
    Money AmountDue) : IDomainEvent;
