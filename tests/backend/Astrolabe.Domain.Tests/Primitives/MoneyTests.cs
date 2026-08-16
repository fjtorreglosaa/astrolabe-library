using Astrolabe.Domain.Primitives;
using FluentAssertions;

namespace Astrolabe.Domain.Tests.Primitives;

/// <summary>
/// Covers BR-GLOBAL-001: money is always a whole number of cents.
/// </summary>
[TestFixture]
public sealed class MoneyTests
{
    [Test]
    public void FromUnits_WithUnitsAndCents_ProducesTotalInCents()
    {
        Money.FromUnits(12, 99).Cents.Should().Be(1299);
    }

    [Test]
    public void FromUnits_WithNegativeUnits_KeepsTheWholeAmountNegative()
    {
        // Guards a classic sign bug: cents must follow the sign of the units, not be added to it.
        Money.FromUnits(-3, 50).Cents.Should().Be(-350);
    }

    [Test]
    public void FromUnits_WhenCentsExceedNinetyNine_Throws()
    {
        var act = () => Money.FromUnits(1, 100);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void Percentage_TruncatesTowardZero()
    {
        // $12.99 at 15% is 194.85 cents. Truncating protects the business from rounding up a discount.
        Money.FromUnits(12, 99).Percentage(15).Cents.Should().Be(194);
    }

    [Test]
    public void Percentage_WithRateOutsideZeroToHundred_Throws()
    {
        var act = () => Money.FromCents(100).Percentage(101);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void Addition_And_Subtraction_OperateOnCents()
    {
        (Money.FromUnits(10) + Money.FromUnits(2, 50)).Cents.Should().Be(1250);
        (Money.FromUnits(10) - Money.FromUnits(2, 50)).Cents.Should().Be(750);
    }

    [Test]
    public void Min_CapsAnAmount()
    {
        // The shape the fine cap relies on: accrued amount capped at a maximum.
        var accrued = Money.FromCents(1200);
        var cap = Money.FromUnits(9);

        accrued.Min(cap).Should().Be(cap);
    }

    [TestCase(1299, "$12.99")]
    [TestCase(0, "$0.00")]
    [TestCase(5, "$0.05")]
    [TestCase(-350, "-$3.50")]
    public void ToString_FormatsForDisplay(long cents, string expected)
    {
        Money.FromCents(cents).ToString().Should().Be(expected);
    }

    [Test]
    public void Equality_IsByValue()
    {
        Money.FromCents(1299).Should().Be(Money.FromUnits(12, 99));
    }
}
