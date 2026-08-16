namespace Astrolabe.Domain.Features.Support.Enums;

/// <summary>
/// What a ticket is about. The prototype's five, verbatim — a closed list because a category nobody
/// can group by is a category nobody can staff for.
/// </summary>
public enum TicketCategory
{
    PaymentsAndFines = 0,
    ReservationsAndReturns = 1,
    CatalogueAndAvailability = 2,
    AccountAndPlan = 3,
    SomethingIsBroken = 4
}
