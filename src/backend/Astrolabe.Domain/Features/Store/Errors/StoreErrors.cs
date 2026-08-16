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

    // ---------- Redemption, BR-STR-007 ----------

    /// <summary>
    /// BR-STR-008. The balance survives a downgrade; the right to spend it does not. Deliberate:
    /// a banked balance is what brings a lapsed member back to Max.
    /// </summary>
    public static readonly Error RedemptionRequiresMaxPlan =
        Error.Conflict("store.redemption_requires_max_plan",
            "Reward points can be spent on the Max plan. Yours are safe until then.");

    public static readonly Error RedemptionInvalid =
        Error.Validation("store.redemption_invalid", "A redemption cannot be negative.");

    public static readonly Error RedemptionBelowMinimum =
        Error.Validation("store.redemption_below_minimum",
            "The smallest redemption is 100 points, worth $1.00.");

    public static readonly Error RedemptionExceedsBalance =
        Error.Validation("store.redemption_exceeds_balance",
            "You do not have that many reward points.");

    /// <summary>
    /// Distinct from exceeding the balance on purpose. A member told "you do not have that many"
    /// when they plainly do would think the balance was wrong; the cap is a rule about the order.
    /// </summary>
    public static readonly Error RedemptionExceedsCap =
        Error.Validation("store.redemption_exceeds_cap",
            "Reward points can cover at most half of a purchase.");
}
