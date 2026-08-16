using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Membership.Enums;
using Astrolabe.Domain.Features.Store.Enums;
using Astrolabe.Domain.Features.Store.Errors;
using Astrolabe.Domain.Features.Store.Events;
using Astrolabe.Domain.Features.Store.Policies;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Store.Entities;

/// <summary>
/// One purchase. Implements BR-STR-010 to BR-STR-015.
///
/// <para>
/// Every total is <b>stored, not recomputed</b>. An order is a receipt: what it says was charged has
/// to stay what was charged, whatever a price or a plan does afterwards. The same reasoning that
/// freezes a fine at assessment.
/// </para>
/// </summary>
public sealed class Order : AggregateRoot
{
    /// <summary>BR-STR-010. The same $3.99 the delivery of a loan costs, added once per order.</summary>
    public static readonly Money ShippingCost = Money.FromUnits(3, 99);

    private readonly List<OrderLine> _lines = [];

    private Order()
    {
    }

    private Order(
        Guid id, Guid memberId, OrderFulfilment fulfilment,
        IEnumerable<OrderLine> lines, PlanTier plan, int pointsRedeemed, string? idempotencyKey,
        DateTimeOffset now) : base(id)
    {
        MemberId = memberId;
        Fulfilment = fulfilment;
        Status = OrderStatus.Paid;
        PlacedAt = now;
        IdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey.Trim();

        _lines.AddRange(lines);

        Subtotal = Money.FromCents(_lines.Sum(line => line.GrossTotal.Cents));
        DiscountTotal = Money.FromCents(_lines.Sum(line => line.DiscountAmount.Cents));

        // BR-STR-010: once per order, however many lines it has.
        ShippingFee = fulfilment is OrderFulfilment.Shipping ? ShippingCost : Money.Zero;

        // The sum of the lines, never a percentage of the subtotal — BR-STR-004.
        var afterDiscount = Money.FromCents(_lines.Sum(line => line.LineTotal.Cents));

        Total = afterDiscount + ShippingFee;

        // BR-STR-007. Points are a tender, not a discount: the order is still worth Total, and the
        // card is asked for the remainder. Recording it as a discount would understate what the
        // member bought and make the receipt disagree with the ledger.
        PointsRedeemed = pointsRedeemed;

        // Earned on what was settled in money, so the shipping fee earns nothing — a member cannot
        // buy points by choosing delivery — and neither does the part paid with points, which would
        // otherwise regenerate themselves. Same principle BR-STR-006 already applies to the
        // discount: a member earns on what they actually spent.
        PointsEarned = RewardPointsPolicy.Earned(
            plan, afterDiscount - Money.FromCents(pointsRedeemed));

        Raise(new OrderPlaced(Guid.NewGuid(), now, id, memberId, Total, PointsEarned, _lines.Count));
    }

    public Guid MemberId { get; private set; }

    public OrderFulfilment Fulfilment { get; private set; }

    public OrderStatus Status { get; private set; }

    /// <summary>The sum of the lines before any discount.</summary>
    public Money Subtotal { get; private set; }

    public Money DiscountTotal { get; private set; }

    public Money ShippingFee { get; private set; }

    /// <summary>What was charged. Stored, because a receipt does not change its mind.</summary>
    public Money Total { get; private set; }

    /// <summary>Point-cents earned. Zero for everyone but Max.</summary>
    public int PointsEarned { get; private set; }

    /// <summary>Point-cents applied to this purchase. BR-STR-007.</summary>
    public int PointsRedeemed { get; private set; }

    /// <summary>
    /// What the card was actually asked for.
    ///
    /// Derived rather than stored, unlike every other total here. The rule those obey is that a
    /// receipt must not change its mind when a price or a plan does — and this is a subtraction of
    /// two values that are themselves already frozen, so there is nothing left to drift.
    /// </summary>
    public Money AmountCharged => Total - Money.FromCents(PointsRedeemed);

    public DateTimeOffset PlacedAt { get; private set; }

    /// <summary>Deduplicates a retried purchase. BR-STR-015.</summary>
    public string? IdempotencyKey { get; private set; }

    public IReadOnlyList<OrderLine> Lines => _lines;

    public static Result<Order> Place(
        Guid memberId,
        OrderFulfilment fulfilment,
        IReadOnlyList<OrderLine> lines,
        PlanTier plan,
        int pointsRedeemed,
        string? idempotencyKey,
        DateTimeOffset now)
    {
        if (lines.Count == 0)
        {
            return Result.Failure<Order>(StoreErrors.NothingToBuy);
        }

        // The balance is the handler's to check — it is not a fact about this order. What the
        // aggregate owns is the cap, which is a pure function of the lines, so no caller can place
        // an order that breaks BR-STR-007 however it was routed here.
        var afterDiscount = Money.FromCents(lines.Sum(line => line.LineTotal.Cents));
        var cap = RewardRedemptionPolicy.CapFor(afterDiscount);

        if (pointsRedeemed < 0)
        {
            return Result.Failure<Order>(StoreErrors.RedemptionInvalid);
        }

        // BR-STR-008, checked here as well as in the handler: the plan is on this order, so the
        // aggregate can enforce it and no route into Place can spend points off a lapsed plan.
        if (pointsRedeemed > 0 && !RewardRedemptionPolicy.CanRedeemOn(plan))
        {
            return Result.Failure<Order>(StoreErrors.RedemptionRequiresMaxPlan);
        }

        if (pointsRedeemed > cap)
        {
            return Result.Failure<Order>(StoreErrors.RedemptionExceedsCap);
        }

        return Result.Success(new Order(
            Guid.NewGuid(), memberId, fulfilment, lines, plan, pointsRedeemed, idempotencyKey, now));
    }

    /// <summary>How the ledger describes this order on the member's statement.</summary>
    public string Description => _lines.Count == 1
        ? $"Purchase — {_lines[0].BookTitle}"
        : $"Purchase · {_lines.Count} titles";
}
