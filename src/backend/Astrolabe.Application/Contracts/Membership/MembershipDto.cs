namespace Astrolabe.Application.Contracts.Membership;

/// <summary>The member's own membership, as the settings screen renders it.</summary>
public sealed record MembershipDto(
    string Plan,
    string Reach,
    int PriceCents,
    int DiscountPercent,
    bool EarnsPoints,
    bool SeesRecommendations,
    DateTimeOffset CycleStartedOn,
    DateTimeOffset RenewsOn,
    int DaysRemaining,
    Guid? CityId,
    string? CityName,
    Guid? HomeLibraryId,
    string? HomeLibraryName,
    ScheduledPlanChangeDto? ScheduledChange,
    bool CanChangeCityThisCycle);
