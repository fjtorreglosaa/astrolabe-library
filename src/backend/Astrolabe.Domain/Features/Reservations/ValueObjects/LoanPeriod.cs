namespace Astrolabe.Domain.Features.Reservations.ValueObjects;

/// <summary>
/// The 14 days a member has a copy. Implements BR-RSV-001 and BR-RSV-010.
/// </summary>
public sealed record LoanPeriod
{
    /// <summary>BR-RSV-001. The prototype states it on the confirmation modal: "Due in 14 days".</summary>
    public const int LoanDays = 14;

    private LoanPeriod(DateTimeOffset startedOn, DateTimeOffset dueOn)
    {
        StartedOn = startedOn;
        DueOn = dueOn;
    }

    public DateTimeOffset StartedOn { get; }

    public DateTimeOffset DueOn { get; }

    public static LoanPeriod StartingAt(DateTimeOffset start) =>
        new(start, start.AddDays(LoanDays));

    /// <summary>Rehydration from storage. The stored dates are the authority, not the constant.</summary>
    public static LoanPeriod FromStoredValues(DateTimeOffset startedOn, DateTimeOffset dueOn) =>
        new(startedOn, dueOn);

    public bool IsOverdueAt(DateTimeOffset now) => now > DueOn;

    /// <summary>
    /// How many days late, rounded <b>up</b> and floored at zero.
    ///
    /// Rounded up because a member one hour past the due date is a day late at the desk, and that is
    /// what the interface must say. Floored at zero because returning early is not negative lateness,
    /// and a negative would flow straight into billing as a credit.
    /// </summary>
    public int DaysLateAt(DateTimeOffset now)
    {
        if (!IsOverdueAt(now))
        {
            return 0;
        }

        return (int)Math.Ceiling((now - DueOn).TotalDays);
    }

    /// <summary>Whole days until the copy is due, floored at zero once it is late.</summary>
    public int DaysRemainingAt(DateTimeOffset now) =>
        IsOverdueAt(now) ? 0 : (int)Math.Ceiling((DueOn - now).TotalDays);
}
