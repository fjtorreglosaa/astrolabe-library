using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Membership.Enums;

namespace Astrolabe.Domain.Features.Membership.Events;

/// <summary>A scheduled downgrade was withdrawn. Carries identifiers and values only.</summary>
public sealed record PlanChangeCancelled(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid MemberId, PlanTier CancelledTarget) : IDomainEvent;
