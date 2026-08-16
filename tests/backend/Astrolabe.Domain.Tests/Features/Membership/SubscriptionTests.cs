using Astrolabe.Domain.Features.Membership.Entities;
using Astrolabe.Domain.Features.Membership.Enums;
using Astrolabe.Domain.Features.Membership.Errors;
using Astrolabe.Domain.Features.Membership.Events;
using Astrolabe.Domain.Features.Membership.ValueObjects;
using FluentAssertions;

namespace Astrolabe.Domain.Tests.Features.Membership;

/// <summary>
/// Covers the subscription aggregate: BR-MBR-011 to BR-MBR-021.
///
/// The upgrade and downgrade rules are opposites — one applies now and charges, the other waits and
/// charges nothing — so most of these tests exist to keep the two from being confused.
/// </summary>
[TestFixture]
public sealed class SubscriptionTests
{
    private static readonly DateTimeOffset CycleStart = new(2026, 8, 12, 0, 0, 0, TimeSpan.Zero);

    private static Subscription AnActiveSubscription(PlanTier plan = PlanTier.Plus)
    {
        var subscription = Subscription.Start(Guid.NewGuid(), plan, CycleStart);
        subscription.ClearDomainEvents();
        return subscription;
    }

    // ---------- Upgrades, BR-MBR-013 to BR-MBR-015 ----------

    [Test]
    public void Upgrade_AppliesImmediately()
    {
        var subscription = AnActiveSubscription(PlanTier.Plus);

        var result = subscription.Upgrade(PlanTier.Max, hasPaymentMethod: true, CycleStart.AddDays(3));

        result.IsSuccess.Should().BeTrue();
        subscription.Plan.Should().Be(PlanTier.Max);
        subscription.ScheduledChange.Should().BeNull();
    }

    [Test]
    public void Upgrade_ChargesTheDifferenceForTheDaysRemaining()
    {
        // AC-MBR-001. Plus is $6.99 and Max $12.99 over a 31-day cycle. Three days in, 28 remain.
        var subscription = AnActiveSubscription(PlanTier.Plus);

        var quote = subscription.Upgrade(PlanTier.Max, true, CycleStart.AddDays(3)).Value;

        quote.Charge.Cents.Should().Be(1173, "1299 × 28 / 31 rounded");
        quote.Credit.Cents.Should().Be(631, "699 × 28 / 31 rounded");
        quote.AmountDue.Cents.Should().Be(542, "the member pays only the difference");
    }

    [Test]
    public void Upgrade_NeverProducesANegativeAmount()
    {
        // AC-MBR-002. A member must never be handed a refund by an upgrade.
        var subscription = AnActiveSubscription(PlanTier.Basic);

        var quote = subscription.Upgrade(PlanTier.Plus, true, CycleStart.AddDays(1)).Value;

        quote.AmountDue.IsNegative.Should().BeFalse();
        quote.AmountDue.Cents.Should().BeGreaterThanOrEqualTo(0);
    }

    [Test]
    public void Upgrade_ToAPaidPlanWithoutAPaymentMethod_IsRefused()
    {
        // AC-MBR-008.
        var subscription = AnActiveSubscription(PlanTier.Basic);

        var result = subscription.Upgrade(PlanTier.Max, hasPaymentMethod: false, CycleStart);

        result.Error.Should().Be(MembershipErrors.PaymentMethodRequired);
        subscription.Plan.Should().Be(PlanTier.Basic);
    }

    [Test]
    public void Upgrade_CancelsAPendingDowngrade()
    {
        // Edge case: holding both would leave the member's future plan ambiguous.
        var subscription = AnActiveSubscription(PlanTier.Plus);
        subscription.ScheduleDowngrade(PlanTier.Basic, CycleStart.AddDays(1));

        subscription.Upgrade(PlanTier.Max, true, CycleStart.AddDays(2));

        subscription.ScheduledChange.Should().BeNull();
        subscription.Plan.Should().Be(PlanTier.Max);
    }

    [Test]
    public void Upgrade_RaisesTheEventCarryingTheAmount()
    {
        var subscription = AnActiveSubscription(PlanTier.Plus);

        subscription.Upgrade(PlanTier.Max, true, CycleStart.AddDays(3));

        subscription.DomainEvents.Should().ContainSingle(e => e is PlanUpgraded);
    }

    [Test]
    public void Upgrade_ToTheSamePlan_Fails()
    {
        AnActiveSubscription(PlanTier.Max)
            .Upgrade(PlanTier.Max, true, CycleStart)
            .Error.Should().Be(MembershipErrors.AlreadyOnThatPlan);
    }

    // ---------- Downgrades, BR-MBR-016 to BR-MBR-019 ----------

    [Test]
    public void Downgrade_DoesNotChangeThePlanNow()
    {
        // AC-MBR-004. The member keeps what they already paid for.
        var subscription = AnActiveSubscription(PlanTier.Max);

        subscription.ScheduleDowngrade(PlanTier.Basic, CycleStart.AddDays(3)).IsSuccess.Should().BeTrue();

        subscription.Plan.Should().Be(PlanTier.Max, "the current plan runs to the renewal date");
        subscription.ScheduledChange!.Target.Should().Be(PlanTier.Basic);
        subscription.ScheduledChange.EffectiveOn.Should().Be(subscription.Cycle.RenewsOn);
    }

    [Test]
    public void Downgrade_ChargesNothing()
    {
        // AC-MBR-005.
        var subscription = AnActiveSubscription(PlanTier.Max);

        var quote = subscription.QuoteChange(PlanTier.Plus, CycleStart.AddDays(3)).Value;

        quote.AmountDue.Should().Be(Domain.Primitives.Money.Zero);
        quote.Charge.Should().Be(Domain.Primitives.Money.Zero);
        quote.EffectiveOn.Should().Be(subscription.Cycle.RenewsOn);
    }

    [Test]
    public void Downgrade_RequestedTwice_ReplacesRatherThanQueues()
    {
        // AC-MBR-007. The member always has exactly one future plan.
        var subscription = AnActiveSubscription(PlanTier.Max);

        subscription.ScheduleDowngrade(PlanTier.Basic, CycleStart.AddDays(1));
        subscription.ScheduleDowngrade(PlanTier.Plus, CycleStart.AddDays(2));

        subscription.ScheduledChange!.Target.Should().Be(PlanTier.Plus);
    }

    [Test]
    public void CancelScheduledChange_LeavesTheMemberOnTheirCurrentPlan()
    {
        // AC-MBR-006.
        var subscription = AnActiveSubscription(PlanTier.Max);
        subscription.ScheduleDowngrade(PlanTier.Basic, CycleStart.AddDays(1));

        subscription.CancelScheduledChange(CycleStart.AddDays(2)).IsSuccess.Should().BeTrue();

        subscription.ScheduledChange.Should().BeNull();
        subscription.Plan.Should().Be(PlanTier.Max);
        subscription.DomainEvents.Should().Contain(e => e is PlanChangeCancelled);
    }

    [Test]
    public void CancelScheduledChange_WithNothingPending_Fails()
    {
        AnActiveSubscription()
            .CancelScheduledChange(CycleStart)
            .Error.Should().Be(MembershipErrors.NoScheduledChange);
    }

    // ---------- Applying a due change, BR-MBR-021 ----------

    [Test]
    public void ApplyDueChange_BeforeTheRenewalDate_DoesNothing()
    {
        var subscription = AnActiveSubscription(PlanTier.Max);
        subscription.ScheduleDowngrade(PlanTier.Basic, CycleStart.AddDays(1));

        var applied = subscription.ApplyDueChange(CycleStart.AddDays(10));

        applied.Value.Should().BeNull();
        subscription.Plan.Should().Be(PlanTier.Max);
    }

    [Test]
    public void ApplyDueChange_AtTheRenewalDate_MovesThePlan()
    {
        var subscription = AnActiveSubscription(PlanTier.Max);
        subscription.ScheduleDowngrade(PlanTier.Basic, CycleStart.AddDays(1));
        var renewal = subscription.Cycle.RenewsOn;

        var applied = subscription.ApplyDueChange(renewal);

        applied.Value.Should().Be(PlanTier.Basic);
        subscription.Plan.Should().Be(PlanTier.Basic);
        subscription.ScheduledChange.Should().BeNull();
        subscription.DomainEvents.Should().Contain(e => e is PlanChangeApplied);
    }

    [Test]
    public void ApplyDueChange_IsIdempotent()
    {
        // Called both on read and by a background sweep, so it must be safe to run twice.
        var subscription = AnActiveSubscription(PlanTier.Max);
        subscription.ScheduleDowngrade(PlanTier.Plus, CycleStart.AddDays(1));
        var renewal = subscription.Cycle.RenewsOn;

        subscription.ApplyDueChange(renewal);
        var second = subscription.ApplyDueChange(renewal);

        second.Value.Should().BeNull();
        subscription.Plan.Should().Be(PlanTier.Plus);
    }

    [Test]
    public void ApplyDueChange_AfterSeveralMissedCycles_RollsForwardToTheRightAnchor()
    {
        // A dormant member must land on their anchor day, not on today.
        var subscription = AnActiveSubscription(PlanTier.Plus);

        subscription.ApplyDueChange(CycleStart.AddMonths(3).AddDays(2));

        subscription.Cycle.AnchorDay.Should().Be(12);
        subscription.Cycle.RenewsOn.Should().BeAfter(CycleStart.AddMonths(3).AddDays(2));
    }

    // ---------- City changes, BR-MBR-011 ----------

    [Test]
    public void RecordCityChange_IsAllowedOncePerCycle()
    {
        var subscription = AnActiveSubscription();

        subscription.RecordCityChange().IsSuccess.Should().BeTrue();
    }

    [Test]
    public void RecordCityChange_ASecondTimeInTheSameCycle_IsRefused()
    {
        // AC-MBR-014. Oscillating inside a paid period is the abuse the limit exists to stop.
        var subscription = AnActiveSubscription();
        subscription.RecordCityChange();

        subscription.RecordCityChange().Error.Should().Be(MembershipErrors.CityChangeLimitReached);
    }

    [Test]
    public void RecordCityChange_AllowanceResetsOnRenewal()
    {
        var subscription = AnActiveSubscription();
        subscription.RecordCityChange();

        subscription.ApplyDueChange(subscription.Cycle.RenewsOn);

        subscription.RecordCityChange().IsSuccess.Should().BeTrue();
    }

    // ---------- Lifecycle ----------

    [Test]
    public void Start_OpensAnActiveSubscriptionAndRaisesTheEvent()
    {
        var subscription = Subscription.Start(Guid.NewGuid(), PlanTier.Basic, CycleStart);

        subscription.IsActive.Should().BeTrue();
        subscription.Plan.Should().Be(PlanTier.Basic);
        subscription.DomainEvents.Should().ContainSingle(e => e is SubscriptionStarted);
    }

    [Test]
    public void AnEndedSubscription_RefusesEveryChange()
    {
        var subscription = AnActiveSubscription();
        subscription.End(CycleStart.AddDays(1));

        subscription.Upgrade(PlanTier.Max, true, CycleStart.AddDays(2))
            .Error.Should().Be(MembershipErrors.SubscriptionEnded);
        subscription.ScheduleDowngrade(PlanTier.Basic, CycleStart.AddDays(2))
            .Error.Should().Be(MembershipErrors.SubscriptionEnded);
        subscription.RecordCityChange()
            .Error.Should().Be(MembershipErrors.SubscriptionEnded);
    }
}
