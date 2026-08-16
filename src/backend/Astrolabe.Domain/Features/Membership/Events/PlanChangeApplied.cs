using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Membership.Enums;

namespace Astrolabe.Domain.Features.Membership.Events;

/// <summary>A scheduled change landed at the renewal date. Carries identifiers and values only.</summary>
public sealed record PlanChangeApplied(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid MemberId, PlanTier From, PlanTier To) : IDomainEvent;
