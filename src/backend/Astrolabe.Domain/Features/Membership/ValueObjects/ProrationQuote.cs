using Astrolabe.Domain.Features.Membership.Enums;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Membership.ValueObjects;

/// <summary>
/// What a plan change costs today. Implements BR-MBR-014.
///
/// <para>
/// The member is charged for the target plan over the days left in the cycle and credited for the
/// days of their current plan already paid, so they never pay twice for the same period. The amount
/// due is floored at zero: this product charges nothing on a downgrade and never refunds, so a
/// negative amount has no meaning here.
/// </para>
///
/// <para>All three amounts are <see cref="Money"/>, so the arithmetic stays in integer cents.</para>
/// </summary>
public sealed record ProrationQuote
{
    private ProrationQuote(
        PlanTier from, PlanTier to, Money charge, Money credit, Money amountDue, DateTimeOffset effectiveOn)
    {
        From = from;
        To = to;
        Charge = charge;
        Credit = credit;
        AmountDue = amountDue;
        EffectiveOn = effectiveOn;
    }

    public PlanTier From { get; }

    public PlanTier To { get; }

    /// <summary>The target plan, prorated over the days remaining.</summary>
    public Money Charge { get; }

    /// <summary>The current plan over the same days, already paid for.</summary>
    public Money Credit { get; }

    /// <summary>What is actually taken today. Never negative.</summary>
    public Money AmountDue { get; }

    /// <summary>When the change takes effect: now for an upgrade, the renewal date for a downgrade.</summary>
    public DateTimeOffset EffectiveOn { get; }

    public bool IsUpgrade => To.IsHigherThan(From);

    public static ProrationQuote ForUpgrade(
        PlanTier from, PlanTier to, Money fromMonthly, Money toMonthly,
        int daysRemaining, int totalDays, DateTimeOffset now)
    {
        // A zero-day cycle would divide by zero. It cannot occur with a real cycle, but the guard
        // keeps the arithmetic total rather than relying on that.
        if (totalDays <= 0)
        {
            return new ProrationQuote(from, to, Money.Zero, Money.Zero, Money.Zero, now);
        }

        var charge = Prorate(toMonthly, daysRemaining, totalDays);
        var credit = Prorate(fromMonthly, daysRemaining, totalDays);
        var due = charge.Cents > credit.Cents ? charge - credit : Money.Zero;

        return new ProrationQuote(from, to, charge, credit, due, now);
    }

    /// <summary>
    /// A downgrade costs nothing and refunds nothing. The quote exists so the confirmation screen can
    /// state the effective date, which is the renewal rather than today.
    /// </summary>
    public static ProrationQuote ForScheduledDowngrade(
        PlanTier from, PlanTier to, DateTimeOffset effectiveOn) =>
        new(from, to, Money.Zero, Money.Zero, Money.Zero, effectiveOn);

    /// <summary>
    /// Each side is rounded independently before subtracting, which is what the prototype does.
    /// Rounding only the difference drifts by a cent at some day counts.
    /// </summary>
    private static Money Prorate(Money monthly, int daysRemaining, int totalDays) =>
        Money.FromCents((long)Math.Round((double)monthly.Cents * daysRemaining / totalDays,
            MidpointRounding.AwayFromZero));
}
