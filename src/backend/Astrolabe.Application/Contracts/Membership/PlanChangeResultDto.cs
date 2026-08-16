namespace Astrolabe.Application.Contracts.Membership;

/// <summary>The outcome of a confirmed plan change.</summary>
public sealed record PlanChangeResultDto(
    string Plan,
    bool AppliedImmediately,
    int AmountChargedCents,
    DateTimeOffset EffectiveOn);
