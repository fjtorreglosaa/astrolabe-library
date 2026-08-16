using Astrolabe.Domain.Features.Membership.ValueObjects;
using FluentAssertions;

namespace Astrolabe.Domain.Tests.Features.Membership;

/// <summary>
/// Covers BR-MBR-025 and BR-MBR-026: anniversary billing anchored to the subscription start date.
///
/// The month-end cases are the whole point of the type. A cycle anchored on the 31st has no 31st to
/// land on in February, and the naive fix — remembering only the date it actually renewed — walks
/// the billing day backwards a little more every short month.
/// </summary>
[TestFixture]
public sealed class BillingCycleTests
{
    [Test]
    public void StartingAt_AnchorsToTheStartDay()
    {
        var cycle = BillingCycle.StartingAt(new DateTimeOffset(2026, 3, 17, 9, 30, 0, TimeSpan.Zero));

        cycle.AnchorDay.Should().Be(17);
        cycle.RenewsOn.Day.Should().Be(17);
        cycle.RenewsOn.Month.Should().Be(4);
    }

    [Test]
    public void ACycleAnchoredOnThe31st_RenewsOnThe28thInFebruary()
    {
        // AC-MBR-012.
        var cycle = BillingCycle.StartingAt(new DateTimeOffset(2026, 1, 31, 0, 0, 0, TimeSpan.Zero));

        cycle.RenewsOn.Day.Should().Be(28, "2026 is not a leap year");
        cycle.RenewsOn.Month.Should().Be(2);
    }

    [Test]
    public void AnAnchorOfThe31st_ReturnsToThe31stAfterAShortMonth()
    {
        // AC-MBR-013. This is the reason the anchor is stored rather than derived.
        var cycle = BillingCycle.StartingAt(new DateTimeOffset(2026, 1, 31, 0, 0, 0, TimeSpan.Zero));

        // The cycle that begins on the clamped 28 February must still renew on 31 March.
        var march = cycle.Next();

        march.AnchorDay.Should().Be(31);
        march.RenewsOn.Day.Should().Be(31);
        march.RenewsOn.Month.Should().Be(3);
    }

    [Test]
    public void ALeapYearFebruary_RenewsOnThe29th()
    {
        var cycle = BillingCycle.StartingAt(new DateTimeOffset(2028, 1, 31, 0, 0, 0, TimeSpan.Zero));

        cycle.RenewsOn.Day.Should().Be(29);
    }

    [Test]
    public void AnAnchorOfThe30th_SurvivesFebruaryToo()
    {
        var cycle = BillingCycle.StartingAt(new DateTimeOffset(2026, 1, 30, 0, 0, 0, TimeSpan.Zero));

        var march = cycle.Next();

        cycle.RenewsOn.Day.Should().Be(28, "February has no 30th");
        march.RenewsOn.Day.Should().Be(30, "the anchor survives the short month");
    }

    [Test]
    public void EachCycleStartsWhereTheLastOneRenewed()
    {
        var cycle = BillingCycle.StartingAt(new DateTimeOffset(2026, 5, 10, 0, 0, 0, TimeSpan.Zero));

        cycle.Next().StartedOn.Should().Be(cycle.RenewsOn, "no day may fall between two cycles");
    }

    [Test]
    public void DaysRemaining_RoundsUpSoAPartialDayStillCounts()
    {
        // A member upgrading at 23:00 has paid for that day and must be charged for it.
        var cycle = BillingCycle.StartingAt(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));

        cycle.DaysRemainingAt(new DateTimeOffset(2026, 6, 30, 23, 0, 0, TimeSpan.Zero))
            .Should().Be(1);
    }

    [Test]
    public void DaysRemaining_AtTheStartIsTheWholeCycle()
    {
        var start = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var cycle = BillingCycle.StartingAt(start);

        cycle.DaysRemainingAt(start).Should().Be(cycle.TotalDays);
    }

    [Test]
    public void DaysRemaining_NeverGoesNegative()
    {
        var cycle = BillingCycle.StartingAt(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));

        cycle.DaysRemainingAt(new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero))
            .Should().BeGreaterThanOrEqualTo(0);
    }

    [Test]
    public void IsDue_OnlyOnceTheRenewalDateArrives()
    {
        var cycle = BillingCycle.StartingAt(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));

        cycle.IsDueAt(cycle.RenewsOn.AddSeconds(-1)).Should().BeFalse();
        cycle.IsDueAt(cycle.RenewsOn).Should().BeTrue();
    }
}
