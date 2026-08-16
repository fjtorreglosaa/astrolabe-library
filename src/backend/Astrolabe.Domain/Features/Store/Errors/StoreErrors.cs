using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Store.Errors;

public static class StoreErrors
{
    public static readonly Error OrderNotFound =
        Error.NotFound("store.order_not_found", "That order does not exist.");

    public static readonly Error OrderNotYours =
        Error.Authorization("store.order_not_yours", "That order is not yours.");

    public static readonly Error NothingToBuy =
        Error.Validation("store.nothing_to_buy", "Add at least one book to the order.");

    public static readonly Error BookNotForSale =
        Error.NotFound("store.book_not_for_sale",
            "That book is not in the catalogue and cannot be bought.");

    public static readonly Error PriceInvalid =
        Error.Validation("store.price_invalid", "A book with no price cannot be sold.");

    public static readonly Error QuantityInvalid =
        Error.Validation("store.quantity_invalid", "A quantity must be greater than zero.");

    /// <summary>
    /// `BR-STR-007` is undefined and `BLOCK-002` is open, so redemption exists nowhere. Named here so
    /// a future caller reaching for it gets a clear answer rather than a silent zero.
    /// </summary>
    public static readonly Error RedemptionNotAvailable =
        Error.Conflict("store.redemption_not_available",
            "Redeeming reward points is not available yet.");
}
