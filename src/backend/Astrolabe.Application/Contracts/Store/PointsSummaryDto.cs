namespace Astrolabe.Application.Contracts.Store;

/// <summary>
/// The member's reward balance and how it got there.
///
/// <c>CanRedeem</c> is false for everyone today: `BR-STR-007` is undefined and `BLOCK-002` is open,
/// so no redemption exists. It travels as a field rather than being assumed by the client, so the
/// day the rule is decided the interface follows the server.
/// </summary>
public sealed record PointsSummaryDto(
    int BalancePointCents,
    bool EarnsPoints,
    bool CanRedeem,
    string Note,
    IReadOnlyList<PointsMovementDto> Recent);

/// <summary>One movement of points.</summary>
public sealed record PointsMovementDto(
    Guid Id,
    int PointCents,
    string Description,
    DateTimeOffset OccurredAt);
