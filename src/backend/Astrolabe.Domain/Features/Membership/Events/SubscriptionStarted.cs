using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Membership.Enums;

namespace Astrolabe.Domain.Features.Membership.Events;

/// <summary>A member's subscription began. Carries identifiers and values only.</summary>
public sealed record SubscriptionStarted(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid MemberId, PlanTier Plan) : IDomainEvent;
