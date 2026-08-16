namespace Astrolabe.Application.Contracts.Store;

/// <summary>
/// What an order would cost, priced by the same policy the purchase uses.
///
/// <c>DiscountNote</c> explains why the percentage is what it is — a Plus member seeing 0% on a book
/// held in another city is entitled to know that is the rule and not a fault.
/// </summary>
/// <param name="PointsBalance">Everything the member has, whether or not this order can absorb it.</param>
/// <param name="MaxRedeemablePointCents">
/// The most this order will accept: the BR-STR-007 cap, the balance, whichever is smaller, and zero
/// when that falls below the minimum. The control the member sees is bounded by this, so the screen
/// never offers a redemption the server would refuse.
/// </param>
/// <param name="PointsRedeemed">What the quote actually applied.</param>
/// <param name="AmountChargedCents">What the card would be asked for.</param>
public sealed record OrderQuoteDto(
    int SubtotalCents,
    int DiscountTotalCents,
    int ShippingFeeCents,
    int TotalCents,
    int PointsBalance,
    int MaxRedeemablePointCents,
    int PointsRedeemed,
    int AmountChargedCents,
    int PointsWouldEarn,
    string DiscountNote,
    string RedemptionNote,
    IReadOnlyList<OrderLineDto> Lines);
