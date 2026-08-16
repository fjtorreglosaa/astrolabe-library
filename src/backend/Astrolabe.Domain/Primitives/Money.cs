namespace Astrolabe.Domain.Primitives;

/// <summary>
/// A monetary amount, always held as a whole number of cents.
/// Enforces BR-GLOBAL-001: no floating point type may ever represent money in this system.
/// The platform is USD only, so no currency code is carried.
/// </summary>
public readonly record struct Money : IComparable<Money>
{
    public static readonly Money Zero = new(0);

    public Money(long cents)
    {
        Cents = cents;
    }

    public long Cents { get; }

    public bool IsZero => Cents == 0;

    public bool IsNegative => Cents < 0;

    public static Money FromCents(long cents) => new(cents);

    /// <summary>
    /// Builds an amount from whole units and cents, for example <c>Money.FromUnits(12, 99)</c> for $12.99.
    /// Deliberately avoids accepting a decimal so no rounding decision is ever implicit.
    /// </summary>
    public static Money FromUnits(long units, int cents = 0)
    {
        if (cents is < 0 or > 99)
        {
            throw new ArgumentOutOfRangeException(nameof(cents), cents, "Cents must be between 0 and 99.");
        }

        return new Money((units * 100) + (units < 0 ? -cents : cents));
    }

    public static Money operator +(Money left, Money right) => new(left.Cents + right.Cents);

    public static Money operator -(Money left, Money right) => new(left.Cents - right.Cents);

    public static Money operator -(Money value) => new(-value.Cents);

    public static Money operator *(Money value, int factor) => new(value.Cents * factor);

    public static bool operator <(Money left, Money right) => left.Cents < right.Cents;

    public static bool operator >(Money left, Money right) => left.Cents > right.Cents;

    public static bool operator <=(Money left, Money right) => left.Cents <= right.Cents;

    public static bool operator >=(Money left, Money right) => left.Cents >= right.Cents;

    /// <summary>
    /// Applies a whole-percent rate, truncating toward zero so a discount never rounds in the
    /// member's favour by accident. The rounding direction is a business decision and is stated here
    /// rather than left to the caller.
    /// </summary>
    public Money Percentage(int percent)
    {
        if (percent is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(percent), percent, "Percent must be between 0 and 100.");
        }

        return new Money(Cents * percent / 100);
    }

    public Money Min(Money other) => Cents <= other.Cents ? this : other;

    public Money Max(Money other) => Cents >= other.Cents ? this : other;

    public int CompareTo(Money other) => Cents.CompareTo(other.Cents);

    /// <summary>Formats for display only. Never use the result for arithmetic.</summary>
    public override string ToString()
    {
        var sign = Cents < 0 ? "-" : string.Empty;
        var absolute = Math.Abs(Cents);
        return $"{sign}${absolute / 100}.{absolute % 100:D2}";
    }
}
