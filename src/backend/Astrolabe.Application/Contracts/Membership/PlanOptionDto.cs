namespace Astrolabe.Application.Contracts.Membership;

/// <summary>
/// One row of the plan comparison table. <c>Direction</c> is <c>"upgrade"</c>, <c>"downgrade"</c> or
/// null for the member's current plan, so the button label is decided once here rather than by a
/// rank comparison repeated in the frontend.
/// </summary>
public sealed record PlanOptionDto(
    string Plan,
    int PriceCents,
    string Reach,
    int DiscountPercent,
    bool EarnsPoints,
    bool SeesRecommendations,
    bool IsCurrent,
    string? Direction);
