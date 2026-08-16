using Astrolabe.Domain.Features.Catalog.ValueObjects;
using Astrolabe.Domain.Features.Membership.Enums;
using Astrolabe.Domain.Features.Membership.ValueObjects;
using Astrolabe.Domain.Features.Store.Entities;
using Astrolabe.Domain.Features.Store.Enums;
using Astrolabe.Domain.Features.Store.Errors;
using Astrolabe.Domain.Features.Store.Events;
using Astrolabe.Domain.Features.Store.Policies;
using Astrolabe.Domain.Primitives;
using FluentAssertions;

namespace Astrolabe.Domain.Tests.Features.Store;

/// <summary>
/// Covers the store domain: BR-STR-001 to BR-STR-006 and BR-STR-009 to BR-STR-018.
///
/// The pricing gets the most attention. A discount rounded in the wrong place makes a receipt that
/// does not add up, and a member who reports that produces a bug nobody can reproduce.
/// </summary>
[TestFixture]
public sealed class StoreTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 16, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid MemberId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid HomeCity = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid OtherCity = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid HomeLibrary = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private static MemberEntitlement Member(PlanTier plan) =>
        PlanCatalog.EntitlementFor(plan, HomeCity, HomeLibrary);

    private static CopyLocation InHomeCity() => new(HomeLibrary, HomeCity, 2);

    private static CopyLocation Elsewhere() => new(Guid.NewGuid(), OtherCity, 3);

    // ---------- The discount table, BR-STR-001 to BR-STR-003 ----------

    [Test]
    public void Basic_PaysTheListPriceWhereverTheBookIs()
    {
        // AC-STR-001.
        PurchaseDiscountPolicy.PercentFor(Member(PlanTier.Basic), [InHomeCity()]).Should().Be(0);
        PurchaseDiscountPolicy.PercentFor(Member(PlanTier.Basic), [Elsewhere()]).Should().Be(0);
    }

    [Test]
    public void Plus_GetsTenPercentOnlyOnABookHeldInTheirCity()
    {
        // AC-STR-002. This is the rule that makes a store policy necessary: the entitlement's own
        // DiscountPercent says 10 in both cases, and would be wrong in the second.
        PurchaseDiscountPolicy.PercentFor(Member(PlanTier.Plus), [InHomeCity()]).Should().Be(10);
        PurchaseDiscountPolicy.PercentFor(Member(PlanTier.Plus), [Elsewhere()]).Should().Be(0);
    }

    [Test]
    public void Plus_GetsTheDiscountIfAnyCopyIsInTheirCity()
    {
        // "Held by a library in their city" is satisfied by one copy; the rest do not matter.
        PurchaseDiscountPolicy
            .PercentFor(Member(PlanTier.Plus), [Elsewhere(), InHomeCity(), Elsewhere()])
            .Should().Be(10);
    }

    [Test]
    public void Max_GetsFifteenPercentWhereverTheBookIsHeld()
    {
        // AC-STR-003.
        PurchaseDiscountPolicy.PercentFor(Member(PlanTier.Max), [Elsewhere()]).Should().Be(15);
        PurchaseDiscountPolicy.PercentFor(Member(PlanTier.Max), [InHomeCity()]).Should().Be(15);
    }

    [Test]
    public void ABookHeldNowhere_EarnsNoDiscountForAnyone()
    {
        // Buying is still allowed — BR-STR-012 — but there is no library holding it to earn on.
        foreach (var plan in new[] { PlanTier.Basic, PlanTier.Plus, PlanTier.Max })
        {
            PurchaseDiscountPolicy.PercentFor(Member(plan), []).Should().Be(0, $"{plan}");
        }
    }

    // ---------- Rounding, BR-STR-009 ----------

    [Test]
    public void ADiscountIsRoundedToTheNearestCent()
    {
        // $9.99 at 15% is 149.85 cents. To the nearest cent is 150.
        PurchaseDiscountPolicy.DiscountOn(Money.FromCents(999), 15).Cents.Should().Be(150);
    }

    [Test]
    public void ADiscountCanNeverExceedThePrice()
    {
        // BR-STR-009. Unreachable at 10 or 15, and guarded so a future percentage cannot make a
        // line negative.
        PurchaseDiscountPolicy.DiscountOn(Money.FromCents(500), 150).Cents.Should().Be(500);
    }

    [TestCase(0)]
    [TestCase(-10)]
    public void ANonPositivePercentageDiscountsNothing(int percent)
    {
        PurchaseDiscountPolicy.DiscountOn(Money.FromCents(999), percent).Should().Be(Money.Zero);
    }

    // ---------- Lines, BR-STR-004 ----------

    private static OrderLine ALine(int cents, int percent, int quantity = 1) =>
        OrderLine.Create(Guid.NewGuid(), "Klara and the Sun", quantity,
            Money.FromCents(cents), percent).Value;

    [Test]
    public void ALineCarriesItsOwnDiscountAndTotal()
    {
        var line = ALine(2600, 15);

        line.UnitPrice.Cents.Should().Be(2600);
        line.DiscountAmount.Cents.Should().Be(390);
        line.LineTotal.Cents.Should().Be(2210);
    }

    [Test]
    public void AQuantityMultipliesBeforeTheDiscount()
    {
        var line = ALine(999, 15, quantity: 3);

        line.GrossTotal.Cents.Should().Be(2997);
        line.DiscountAmount.Cents.Should().Be(450, "2997 × 15% is 449.55, rounded to 450");
        line.LineTotal.Cents.Should().Be(2547);
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void ALineWithoutAQuantityIsRefused(int quantity)
    {
        OrderLine.Create(Guid.NewGuid(), "A book", quantity, Money.FromCents(999), 0)
            .Error.Should().Be(StoreErrors.QuantityInvalid);
    }

    [Test]
    public void ALineWithNoPriceIsRefused()
    {
        OrderLine.Create(Guid.NewGuid(), "A book", 1, Money.Zero, 0)
            .Error.Should().Be(StoreErrors.PriceInvalid);
    }

    [Test]
    public void ALineWithoutATitleStillReadsAsSomething()
    {
        OrderLine.Create(Guid.NewGuid(), "   ", 1, Money.FromCents(999), 0)
            .Value.BookTitle.Should().Be("Unknown title");
    }

    // ---------- Orders ----------

    private static Order AnOrder(
        PlanTier plan, OrderFulfilment fulfilment, params OrderLine[] lines) =>
        Order.Place(MemberId, fulfilment, lines, plan, null, Now).Value;

    [Test]
    public void TheTotalIsTheSumOfTheLines_NotAPercentageOfTheSubtotal()
    {
        // AC-STR-004. Three lines of $9.99 at 15%: each discounts to 849, so the total is 2547.
        // Discounting the 2997 subtotal once gives 2548 — a cent adrift, and a receipt that does
        // not add up.
        var order = AnOrder(PlanTier.Max, OrderFulfilment.Collection,
            ALine(999, 15), ALine(999, 15), ALine(999, 15));

        order.Subtotal.Cents.Should().Be(2997);
        order.DiscountTotal.Cents.Should().Be(450, "150 per line, three times");
        order.Total.Cents.Should().Be(2547);
        order.Total.Cents.Should().Be(order.Lines.Sum(line => line.LineTotal.Cents));
    }

    [Test]
    public void ShippingIsAddedOncePerOrder_NotPerLine()
    {
        // AC-STR-005.
        var order = AnOrder(PlanTier.Basic, OrderFulfilment.Shipping,
            ALine(1000, 0), ALine(1000, 0), ALine(1000, 0));

        order.ShippingFee.Cents.Should().Be(399);
        order.Total.Cents.Should().Be(3399);
    }

    [Test]
    public void CollectionIsFree()
    {
        AnOrder(PlanTier.Basic, OrderFulfilment.Collection, ALine(1000, 0))
            .ShippingFee.Should().Be(Money.Zero);
    }

    [Test]
    public void AnOrderWithNoLinesIsRefused()
    {
        Order.Place(MemberId, OrderFulfilment.Collection, [], PlanTier.Max, null, Now)
            .Error.Should().Be(StoreErrors.NothingToBuy);
    }

    [Test]
    public void TheDescriptionNamesOneTitleOrCountsSeveral()
    {
        AnOrder(PlanTier.Max, OrderFulfilment.Collection, ALine(999, 0))
            .Description.Should().Be("Purchase — Klara and the Sun");
        AnOrder(PlanTier.Max, OrderFulfilment.Collection, ALine(999, 0), ALine(999, 0))
            .Description.Should().Be("Purchase · 2 titles");
    }

    [Test]
    public void PlacingAnOrderRaisesTheEventCarryingTheTotal()
    {
        var order = AnOrder(PlanTier.Max, OrderFulfilment.Collection, ALine(15000, 15));

        order.DomainEvents.OfType<OrderPlaced>().Single()
            .Total.Should().Be(order.Total);
    }

    // ---------- Reward points, BR-STR-005 and BR-STR-006 ----------

    [Test]
    public void AMaxOrderOfOneHundredAndFiftyDollarsAtFifteenPercent_Accrues85PointCents()
    {
        // AC-STR-006, and the number PLAN-001 disagrees with. $150 less 15% is $127.50, and
        // $127.50 ÷ $1.50 is 85. The plan's $1.00 is $150 ÷ $1.50 with the discount never applied;
        // BR-STR-006 says post-discount, so 85 is what the rule produces. Recorded in
        // store.business.md section 8 and awaiting confirmation.
        var order = AnOrder(PlanTier.Max, OrderFulfilment.Collection, ALine(15_000, 15));

        order.Total.Cents.Should().Be(12_750);
        order.PointsEarned.Should().Be(85);
    }

    [TestCase(PlanTier.Basic)]
    [TestCase(PlanTier.Plus)]
    public void OnlyMaxAccrues(PlanTier plan)
    {
        // AC-STR-007.
        AnOrder(plan, OrderFulfilment.Collection, ALine(15_000, 0)).PointsEarned.Should().Be(0);
    }

    [Test]
    public void AccrualIsTruncatedDownward()
    {
        // $1.49 earns nothing; $1.50 earns one; $2.99 still earns one. Rounding always favours the
        // library, which is the safe direction for a liability.
        RewardPointsPolicy.Earned(PlanTier.Max, Money.FromCents(149)).Should().Be(0);
        RewardPointsPolicy.Earned(PlanTier.Max, Money.FromCents(150)).Should().Be(1);
        RewardPointsPolicy.Earned(PlanTier.Max, Money.FromCents(299)).Should().Be(1);
        RewardPointsPolicy.Earned(PlanTier.Max, Money.FromCents(300)).Should().Be(2);
    }

    [Test]
    public void TheShippingFeeDoesNotEarnPoints()
    {
        // A member must not be able to buy points by choosing delivery. The fee is a service, not
        // spend on books.
        var collected = AnOrder(PlanTier.Max, OrderFulfilment.Collection, ALine(15_000, 15));
        var shipped = AnOrder(PlanTier.Max, OrderFulfilment.Shipping, ALine(15_000, 15));

        shipped.Total.Cents.Should().Be(collected.Total.Cents + 399);
        shipped.PointsEarned.Should().Be(collected.PointsEarned);
    }

    [Test]
    public void NothingSpentEarnsNothing()
    {
        RewardPointsPolicy.Earned(PlanTier.Max, Money.Zero).Should().Be(0);
        RewardPointsPolicy.Earned(PlanTier.Max, Money.FromCents(-500)).Should().Be(0);
    }

    // ---------- Points movements, BR-STR-018 ----------

    [Test]
    public void AnEarnedMovementIsAlwaysPositive()
    {
        PointsMovement.Earned(MemberId, -85, "Order", Guid.NewGuid(), Now)
            .PointCents.Should().Be(85);
    }

    [Test]
    public void APointsBalanceIsTheSumOfItsMovements()
    {
        // AC-STR-012. Points are value, so they get a ledger rather than a mutable number.
        var movements = new[]
        {
            PointsMovement.Earned(MemberId, 85, "Order", Guid.NewGuid(), Now),
            PointsMovement.Earned(MemberId, 40, "Order", Guid.NewGuid(), Now),
        };

        movements.Sum(movement => movement.PointCents).Should().Be(125);
    }

    [Test]
    public void APointsMovementExposesNoWayToChangeItself()
    {
        // The same guard the ledger has, for the same reason: points are money by another name.
        typeof(PointsMovement).GetProperties()
            .Where(property => property.SetMethod?.IsPublic == true)
            .Should().BeEmpty();

        typeof(PointsMovement).GetMethods()
            .Where(method => method.IsPublic && !method.IsStatic)
            .Where(method => method.Name.StartsWith("Set") || method.Name is "Update" or "Delete")
            .Should().BeEmpty();
    }
}
