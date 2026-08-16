namespace Astrolabe.Application.Contracts.Membership;

/// <summary>A downgrade awaiting the renewal date.</summary>
public sealed record ScheduledPlanChangeDto(
    string Target,
    DateTimeOffset EffectiveOn,
    DateTimeOffset RequestedAt);
