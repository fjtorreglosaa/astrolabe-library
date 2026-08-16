using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Membership.Enums;

namespace Astrolabe.Domain.Features.Catalog.Events;

/// <summary>A draft entered the catalogue. Carries identifiers and values only.</summary>
public sealed record BookPublished(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid BookId,
    string Title,
    PlanTier Tier) : IDomainEvent;
