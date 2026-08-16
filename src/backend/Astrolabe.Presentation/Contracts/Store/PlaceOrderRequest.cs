using Astrolabe.Domain.Features.Store.Enums;

namespace Astrolabe.Presentation.Contracts.Store;

/// <summary>
/// The body of a purchase. The member comes from the token, and the prices come from the catalogue —
/// a caller cannot name what a book costs.
/// </summary>
/// <param name="PointsToRedeem">
/// Reward point-cents to put toward this purchase. BR-STR-007 caps it at half the book total and
/// refuses anything under 100. Defaults to zero, so an older client that never sends it simply pays
/// by card.
/// </param>
public sealed record PlaceOrderRequest(
    IReadOnlyList<OrderLineRequestBody> Lines,
    OrderFulfilment Fulfilment,
    Guid PaymentMethodId,
    string? IdempotencyKey,
    int PointsToRedeem = 0);
