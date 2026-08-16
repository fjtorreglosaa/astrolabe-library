using Astrolabe.Domain.Features.Store.Enums;

namespace Astrolabe.Presentation.Contracts.Store;

/// <summary>
/// The body of a purchase. The member comes from the token, and the prices come from the catalogue —
/// a caller cannot name what a book costs.
/// </summary>
public sealed record PlaceOrderRequest(
    IReadOnlyList<OrderLineRequestBody> Lines,
    OrderFulfilment Fulfilment,
    Guid PaymentMethodId,
    string? IdempotencyKey);

/// <summary>One book and how many of it.</summary>
public sealed record OrderLineRequestBody(Guid BookId, int Quantity);
