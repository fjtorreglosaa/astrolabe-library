namespace Astrolabe.Domain.Features.Membership.ValueObjects;

/// <summary>
/// The monthly period a subscription is paid for. Implements BR-MBR-025 and BR-MBR-026.
///
/// <para>
/// Anchored to the day of the month the subscription started — anniversary billing — rather than a
/// calendar day shared by every member. A fixed day would force a prorated first cycle on everyone
/// and concentrate every renewal on one date.
/// </para>
/// </summary>
public sealed record BillingCycle
{
    private BillingCycle(DateTimeOffset startedOn, DateTimeOffset renewsOn, int anchorDay)
    {
        StartedOn = startedOn;
        RenewsOn = renewsOn;
        AnchorDay = anchorDay;
    }

    public DateTimeOffset StartedOn { get; }

    public DateTimeOffset RenewsOn { get; }

    /// <summary>
    /// The day of the month this cycle bills on, remembered independently of
    /// <see cref="RenewsOn"/>.
    ///
    /// A cycle anchored on the 31st renews on 28 February and must return to the 31st in March.
    /// Deriving the anchor from the last renewal date would walk the billing day backwards, a day
    /// at a time, every short month.
    /// </summary>
    public int AnchorDay { get; }

    public int TotalDays => (int)Math.Round((RenewsOn - StartedOn).TotalDays);

    public static BillingCycle StartingAt(DateTimeOffset start)
    {
        var anchor = start.Day;
        return new BillingCycle(start, AddMonthClamped(start, anchor), anchor);
    }

    /// <summary>Rehydrates a stored cycle. Used by the persistence layer only.</summary>
    public static BillingCycle FromStoredValues(DateTimeOffset startedOn, DateTimeOffset renewsOn, int anchorDay) =>
        new(startedOn, renewsOn, anchorDay);

    /// <summary>The cycle that follows this one, keeping the same anchor.</summary>
    public BillingCycle Next() => new(RenewsOn, AddMonthClamped(RenewsOn, AnchorDay), AnchorDay);

    /// <summary>
    /// Whole days left before renewal, never negative.
    ///
    /// Rounded up so a member who upgrades partway through a day is charged for that day rather than
    /// receiving it free — the same direction the prototype's day count uses.
    /// </summary>
    public int DaysRemainingAt(DateTimeOffset now)
    {
        if (now >= RenewsOn)
        {
            return 0;
        }

        return Math.Max(0, (int)Math.Ceiling((RenewsOn - now).TotalDays));
    }

    public bool IsDueAt(DateTimeOffset now) => now >= RenewsOn;

    /// <summary>
    /// Adds one month, landing on the anchor day when the target month has one and on its last day
    /// when it does not.
    /// </summary>
    private static DateTimeOffset AddMonthClamped(DateTimeOffset from, int anchorDay)
    {
        var next = from.AddMonths(1);
        var daysInMonth = DateTime.DaysInMonth(next.Year, next.Month);
        var day = Math.Min(anchorDay, daysInMonth);

        return new DateTimeOffset(
            next.Year, next.Month, day, next.Hour, next.Minute, next.Second, next.Offset);
    }
}
