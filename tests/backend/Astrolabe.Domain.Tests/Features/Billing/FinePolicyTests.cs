using Astrolabe.Domain.Features.Billing.Policies;
using Astrolabe.Domain.Primitives;
using FluentAssertions;

namespace Astrolabe.Domain.Tests.Features.Billing;

/// <summary>
/// Covers BR-BIL-001 and BR-BIL-002 — the rate and the cap.
///
/// These two numbers price every late return in the network. The policy is a pure function precisely
/// so the whole range can be swept here rather than sampled, because an off-by-one would not show up
/// as a crash: it would show up as everybody's bill being slightly wrong.
/// </summary>
[TestFixture]
public sealed class FinePolicyTests
{
    [Test]
    public void TwentyDaysOverdue_IsExactlySevenDollars()
    {
        // AC-BIL-001, stated in PLAN-001 itself. The prototype's seed agrees: "20 days late" is 700.
        FinePolicy.For(20).Cents.Should().Be(700);
    }

    [Test]
    public void ElevenDaysOverdue_MatchesTheSeededFine()
    {
        // The prototype's second seeded fine: "11 days late" is 385.
        FinePolicy.For(11).Cents.Should().Be(385);
    }

    [Test]
    public void TwentySixDays_IsCappedAtNineDollars()
    {
        // AC-BIL-002. 26 × 35 is 910, which the cap trims.
        FinePolicy.For(26).Should().Be(FinePolicy.Cap);
        FinePolicy.For(26).Cents.Should().Be(900);
    }

    [Test]
    public void TwentyFiveDays_IsNotYetCapped()
    {
        // AC-BIL-003. The cap must not bite early — that would under-charge a whole day.
        FinePolicy.For(25).Cents.Should().Be(875);
    }

    [Test]
    public void TheCapIsACapAndNotARateChange()
    {
        // AC-BIL-002. A member 200 days late owes the same as one 26 days late.
        FinePolicy.For(200).Should().Be(FinePolicy.Cap);
        FinePolicy.For(10_000).Should().Be(FinePolicy.Cap);
    }

    [Test]
    public void TheCapBindsFromTheTwentySixthDay()
    {
        FinePolicy.DaysToReachCap.Should().Be(26);
        FinePolicy.For(FinePolicy.DaysToReachCap - 1).Should().NotBe(FinePolicy.Cap);
        FinePolicy.For(FinePolicy.DaysToReachCap).Should().Be(FinePolicy.Cap);
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(-500)]
    public void NothingLate_CostsNothing(int daysLate)
    {
        // BR-BIL-009. A negative — which reservations already floors — must never become a credit.
        FinePolicy.For(daysLate).Should().Be(Money.Zero);
    }

    [Test]
    public void EveryDayUpToTheCapIsExactlyThirtyFiveCentsMoreThanTheLast()
    {
        // Swept rather than sampled: the failure mode of a rate mistake is a bill that is slightly
        // wrong for everybody, which no single example would reveal.
        for (var day = 1; day < FinePolicy.DaysToReachCap; day++)
        {
            FinePolicy.For(day).Cents.Should().Be(day * 35, $"day {day}");
            (FinePolicy.For(day).Cents - FinePolicy.For(day - 1).Cents).Should().Be(35);
        }
    }

    [Test]
    public void AFineNeverExceedsTheCapAtAnyDayCount()
    {
        for (var day = 0; day <= 400; day++)
        {
            FinePolicy.For(day).Cents.Should().BeLessThanOrEqualTo(FinePolicy.Cap.Cents, $"day {day}");
            FinePolicy.For(day).IsNegative.Should().BeFalse($"day {day}");
        }
    }

    [Test]
    public void TheRateAndCapAreTheNumbersTheProductStates()
    {
        // Pinned so a refactor cannot quietly re-price the network. The prototype's help text says
        // "$0.35 a day per title".
        FinePolicy.PerDay.Cents.Should().Be(35);
        FinePolicy.Cap.Cents.Should().Be(900);
    }
}
