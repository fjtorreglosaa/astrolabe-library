using Astrolabe.Domain.Features.Membership.Enums;

namespace Astrolabe.Domain.Features.Membership.ValueObjects;

/// <summary>
/// A downgrade that has been requested but not yet applied. Implements BR-MBR-016 and BR-MBR-018.
/// </summary>
public sealed record ScheduledPlanChange(
    PlanTier Target,
    DateTimeOffset EffectiveOn,
    DateTimeOffset RequestedAt)
{
    public bool IsDueAt(DateTimeOffset now) => now >= EffectiveOn;
}
