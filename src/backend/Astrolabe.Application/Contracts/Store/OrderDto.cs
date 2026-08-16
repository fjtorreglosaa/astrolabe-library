namespace Astrolabe.Application.Contracts.Store;

/// <summary>An order as the purchases screen renders it. A receipt: every figure is what was charged.</summary>
/// <param name="TotalCents">What the order was worth, before any reward points were applied.</param>
/// <param name="PointsRedeemed">Point-cents put toward it. BR-STR-007.</param>
/// <param name="AmountChargedCents">What the card was asked for: the total less the points applied.</param>
public sealed record OrderDto(
    Guid Id,
    string Fulfilment,
    int SubtotalCents,
    int DiscountTotalCents,
    int ShippingFeeCents,
    int TotalCents,
    int PointsRedeemed,
    int AmountChargedCents,
    int PointsEarned,
    DateTimeOffset PlacedAt,
    string Description,
    IReadOnlyList<OrderLineDto> Lines);
