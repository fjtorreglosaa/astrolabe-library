namespace Astrolabe.Application.Contracts.Store;

/// <summary>An order as the purchases screen renders it. A receipt: every figure is what was charged.</summary>
public sealed record OrderDto(
    Guid Id,
    string Fulfilment,
    int SubtotalCents,
    int DiscountTotalCents,
    int ShippingFeeCents,
    int TotalCents,
    int PointsEarned,
    DateTimeOffset PlacedAt,
    string Description,
    IReadOnlyList<OrderLineDto> Lines);
