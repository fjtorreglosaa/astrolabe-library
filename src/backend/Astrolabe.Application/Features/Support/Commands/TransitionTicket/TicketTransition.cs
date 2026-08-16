namespace Astrolabe.Application.Features.Support.Commands.TransitionTicket;

/// <summary>
/// What a staff user is doing to a ticket's state.
///
/// One command with three transitions rather than three commands: they share the scope check, the
/// staff check and the audit write, and differ by one call on the aggregate. Three handlers would be
/// three places for BR-SUP-010 to drift.
/// </summary>
public enum TicketTransition
{
    /// <summary>BR-SUP-003. Assigning is what moves a ticket into review.</summary>
    Assign = 0,
    Resolve = 1,

    /// <summary>BR-SUP-007. Clears the rating with it.</summary>
    Reopen = 2
}
