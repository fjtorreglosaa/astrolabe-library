namespace Astrolabe.Application.Contracts.Store;

/// <summary>One movement of points. Positive earned, negative redeemed.</summary>
public sealed record PointsMovementDto(
    Guid Id,
    int PointCents,
    string Description,
    DateTimeOffset OccurredAt);
