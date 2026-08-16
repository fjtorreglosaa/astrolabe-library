namespace Astrolabe.Application.Contracts.Store;

/// <summary>
/// What an order would cost, priced by the same policy the purchase uses.
///
/// <c>DiscountNote</c> explains why the percentage is what it is — a Plus member seeing 0% on a book
/// held in another city is entitled to know that is the rule and not a fault.
/// </summary>
public sealed record OrderQuoteDto(
    int SubtotalCents,
    int DiscountTotalCents,
    int ShippingFeeCents,
    int TotalCents,
    int PointsWouldEarn,
    string DiscountNote,
    IReadOnlyList<OrderLineDto> Lines);
