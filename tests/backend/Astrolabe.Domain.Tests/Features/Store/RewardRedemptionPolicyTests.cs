using Astrolabe.Domain.Features.Membership.Enums;
using Astrolabe.Domain.Features.Store.Entities;
using Astrolabe.Domain.Features.Store.Enums;
using Astrolabe.Domain.Features.Store.Errors;
using Astrolabe.Domain.Features.Store.Policies;
using Astrolabe.Domain.Primitives;
using FluentAssertions;

namespace Astrolabe.Domain.Tests.Features.Store;

/// <summary>
/// Covers BR-STR-007, defined 2026-08-16 (`GLOBAL-009`), and the clause it added to BR-STR-006.
///
/// <para>
/// Points are money by another name, so these read like the money tests: the cap, the floor, the
/// balance, and the one rule that keeps the programme from feeding itself.
/// </para>
/// </summary>
[TestFixture]
public sealed class RewardRedemptionPolicyTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 16, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid MemberId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    // ---------- The cap, BR-STR-007 ----------

    [Test]
    public void TheCapIsHalfTheBookTotal()
    {
        RewardRedemptionPolicy.CapFor(Money.FromUnits(45)).Should().Be(2250);
    }

    [Test]
    public void TheCapRoundsDown_NeverUp()
    {
        // An odd number of cents cannot be halved exactly. It rounds toward the library, which is
        // the safe direction for a liability — the same choice BR-STR-006 makes when earning.
        RewardRedemptionPolicy.CapFor(Money.FromCents(999)).Should().Be(499);
    }

    [Test]
    public void NothingCanBeRedeemedAgainstAnEmptyOrder()
    {
        RewardRedemptionPolicy.CapFor(Money.Zero).Should().Be(0);
    }

    // ---------- What a member may actually apply ----------

    [Test]
    public void TheBalanceLimitsTheRedemptionWhenItIsSmallerThanTheCap()
    {
        // $45 allows 2250; the member has 300.
        RewardRedemptionPolicy.MaxRedeemable(PlanTier.Max, 300, Money.FromUnits(45)).Should().Be(300);
    }

    [Test]
    public void TheCapLimitsTheRedemptionWhenTheBalanceIsLarger()
    {
        RewardRedemptionPolicy.MaxRedeemable(PlanTier.Max, 9000, Money.FromUnits(45)).Should().Be(2250);
    }

    [Test]
    public void AnAmountBelowTheFloorIsOfferedAsNothingAtAll()
    {
        // Offering someone 40 points they are not allowed to spend is worse than offering none:
        // the control would be there, and using it would fail.
        RewardRedemptionPolicy.MaxRedeemable(PlanTier.Max, 40, Money.FromUnits(45)).Should().Be(0);
    }

    [Test]
    public void ASmallPurchaseCanBeTooSmallToSpendPointsOn()
    {
        // $1.50 allows 75, under the 100 floor — so a large balance still buys nothing here.
        RewardRedemptionPolicy.MaxRedeemable(PlanTier.Max, 5000, Money.FromUnits(1, 50)).Should().Be(0);
    }

    // ---------- Judging a request ----------

    [Test]
    public void RedeemingNothingIsAlwaysValid()
    {
        RewardRedemptionPolicy.EnsureValid(PlanTier.Max, 0, 0, Money.Zero).IsSuccess.Should().BeTrue();
    }

    [Test]
    public void ANegativeRedemptionIsRefused()
    {
        // Otherwise a caller could mint points by "redeeming" a negative amount.
        RewardRedemptionPolicy.EnsureValid(PlanTier.Max, -500, 5000, Money.FromUnits(45))
            .Error.Should().Be(StoreErrors.RedemptionInvalid);
    }

    [Test]
    public void ARedemptionUnderTheFloorIsRefused()
    {
        RewardRedemptionPolicy.EnsureValid(PlanTier.Max, 99, 5000, Money.FromUnits(45))
            .Error.Should().Be(StoreErrors.RedemptionBelowMinimum);
    }

    [Test]
    public void ARedemptionBeyondTheBalanceIsRefused()
    {
        RewardRedemptionPolicy.EnsureValid(PlanTier.Max, 500, 400, Money.FromUnits(45))
            .Error.Should().Be(StoreErrors.RedemptionExceedsBalance);
    }

    [Test]
    public void ARedemptionBeyondTheCapIsRefusedDistinctly()
    {
        // Not "you do not have that many": the member plainly does, and being told otherwise would
        // make them think the balance was wrong rather than the amount.
        RewardRedemptionPolicy.EnsureValid(PlanTier.Max, 3000, 9000, Money.FromUnits(45))
            .Error.Should().Be(StoreErrors.RedemptionExceedsCap);
    }

    [Test]
    public void ExactlyTheCapIsAllowed()
    {
        RewardRedemptionPolicy.EnsureValid(PlanTier.Max, 2250, 9000, Money.FromUnits(45))
            .IsSuccess.Should().BeTrue();
    }

    // ---------- The order honours it ----------

    private static Order AnOrder(int pointsRedeemed, PlanTier plan = PlanTier.Max) =>
        Order.Place(
            MemberId, OrderFulfilment.Collection,
            [OrderLine.Create(Guid.NewGuid(), "Any title", 1, Money.FromUnits(45), 0).Value],
            plan, pointsRedeemed, null, Now).Value;

    [Test]
    public void TheOrderTotalDoesNotMove_BecausePointsAreATenderNotADiscount()
    {
        // The member still bought $45 of books. Recording it as a discount would understate the
        // purchase and make the receipt disagree with the ledger.
        var order = AnOrder(pointsRedeemed: 1000);

        order.Total.Should().Be(Money.FromUnits(45));
        order.PointsRedeemed.Should().Be(1000);
    }

    [Test]
    public void TheCardIsAskedForTheRemainder()
    {
        AnOrder(pointsRedeemed: 1000).AmountCharged.Should().Be(Money.FromUnits(35));
    }

    [Test]
    public void ThePartPaidWithPointsEarnsNothing()
    {
        // The rule that stops the programme feeding itself. $45 less $10 of points is $35, and
        // $35 ÷ $1.50 is 23 point-cents — not the 30 the full total would have earned.
        AnOrder(pointsRedeemed: 1000).PointsEarned.Should().Be(23);
    }

    [Test]
    public void AnOrderPaidEntirelyByCardEarnsOnTheWholeTotal()
    {
        AnOrder(pointsRedeemed: 0).PointsEarned.Should().Be(30);
    }

    [Test]
    public void TheAggregateRefusesARedemptionOverTheCap_HoweverItWasRouted()
    {
        // Defence in depth. The handler checks the balance because that is a fact about the member;
        // the cap is a pure function of these lines, so the aggregate can and does refuse it itself.
        Order.Place(
            MemberId, OrderFulfilment.Collection,
            [OrderLine.Create(Guid.NewGuid(), "Any title", 1, Money.FromUnits(45), 0).Value],
            PlanTier.Max, pointsRedeemed: 3000, null, Now)
            .Error.Should().Be(StoreErrors.RedemptionExceedsCap);
    }

    [Test]
    public void DeliveryCannotBePaidForWithPoints()
    {
        // The cap is measured on the books alone. A $1.50 book plus $3.99 delivery is $5.49, but
        // only the $1.50 counts — so 75, which is under the floor, and nothing may be redeemed.
        var cap = RewardRedemptionPolicy.CapFor(Money.FromUnits(1, 50));

        cap.Should().Be(75);
        RewardRedemptionPolicy.MaxRedeemable(PlanTier.Max, 9000, Money.FromUnits(1, 50)).Should().Be(0);
    }

    // ---------- BR-STR-008: spending needs an active Max plan ----------

    [TestCase(PlanTier.Basic)]
    [TestCase(PlanTier.Plus)]
    public void APlanBelowMax_MayNotSpendPoints(PlanTier plan)
    {
        // Deliberate, and not the same as forfeiting them. BR-STR-008 keeps the balance and
        // suspends the right to spend it — a banked balance is what brings a lapsed member back.
        RewardRedemptionPolicy.CanRedeemOn(plan).Should().BeFalse();

        RewardRedemptionPolicy.EnsureValid(plan, 1_000, 5_000, Money.FromUnits(45))
            .Error.Should().Be(StoreErrors.RedemptionRequiresMaxPlan);
    }

    [TestCase(PlanTier.Basic)]
    [TestCase(PlanTier.Plus)]
    public void APlanBelowMax_IsOfferedNothingToSpend(PlanTier plan)
    {
        RewardRedemptionPolicy.MaxRedeemable(plan, 5_000, Money.FromUnits(45)).Should().Be(0);
    }

    [TestCase(PlanTier.Basic)]
    [TestCase(PlanTier.Plus)]
    public void APlanBelowMax_MayStillBuyWithoutSpendingPoints(PlanTier plan)
    {
        // The refusal is about redeeming, not about buying. A Plus member must still be able to
        // shop, and BR-STR-008 does not touch the balance itself.
        RewardRedemptionPolicy.EnsureValid(plan, 0, 5_000, Money.FromUnits(45))
            .IsSuccess.Should().BeTrue();

        AnOrder(pointsRedeemed: 0, plan).PointsRedeemed.Should().Be(0);
    }

    [Test]
    public void TheAggregateRefusesARedemptionOffALapsedPlan()
    {
        Order.Place(
            MemberId, OrderFulfilment.Collection,
            [OrderLine.Create(Guid.NewGuid(), "Any title", 1, Money.FromUnits(45), 0).Value],
            PlanTier.Plus, pointsRedeemed: 1_000, null, Now)
            .Error.Should().Be(StoreErrors.RedemptionRequiresMaxPlan);
    }

    [Test]
    public void PointsMovementRecordsARedemptionAsNegative()
    {
        // The balance stays a plain sum, so no reader has to know which kinds subtract.
        var movement = PointsMovement.Redeemed(MemberId, 1000, "Purchase", Guid.NewGuid(), Now);

        movement.PointCents.Should().Be(-1000);
    }
}
