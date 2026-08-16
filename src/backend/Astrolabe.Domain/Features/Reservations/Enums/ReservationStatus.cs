namespace Astrolabe.Domain.Features.Reservations.Enums;

/// <summary>
/// Where a reservation sits in the loan cycle.
///
/// <para>
/// <b>There is deliberately no <c>Overdue</c> member.</b> Overdue is a function of the due date and
/// the clock, exposed as <c>Reservation.IsOverdueAt</c>. A stored flag would need a job to maintain
/// it, and the day that job fails every late loan silently reads as current.
/// </para>
/// </summary>
public enum ReservationStatus
{
    /// <summary>The member holds the copy.</summary>
    Reserved = 0,

    /// <summary>The member has handed it over; the library has not yet received it.</summary>
    InTransit = 1,

    /// <summary>Library staff checked the copy in. The only state that completes a loan.</summary>
    Returned = 2,

    /// <summary>A confirmation that failed after taking stock. Unreachable from the interface.</summary>
    Cancelled = 3
}
