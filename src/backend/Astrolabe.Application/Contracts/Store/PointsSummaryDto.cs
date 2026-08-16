namespace Astrolabe.Application.Contracts.Store;

/// <summary>
/// The member's reward balance and how it got there.
///
/// <c>CanRedeem</c> travels as a field rather than being assumed by the client. It became true in
/// <c>STR-017</c>, when BR-STR-007 was defined and `BLOCK-002` closed.
/// </summary>
public sealed record PointsSummaryDto(
    int BalancePointCents,
    bool EarnsPoints,
    bool CanRedeem,
    string Note,
    IReadOnlyList<PointsMovementDto> Recent);
