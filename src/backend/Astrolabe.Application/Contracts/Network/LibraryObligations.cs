namespace Astrolabe.Application.Contracts.Network;

/// <summary>
/// What a library still holds: stock on its shelves, loans out with members, and money owed.
///
/// <para>
/// A <b>report, not a verdict</b>. It used to be a single <c>bool</c> that refused deactivation, and
/// that inverted <c>BR-NET-005</c> — the rule blocks *deletion* on these facts and offers
/// deactivation as the safe alternative that preserves history. Refusing on them also deadlocked:
/// copies are permanent stock and fresh reservations keep arriving until the library stops taking
/// them, so the condition for withdrawing a library was one that continued operation regenerated.
/// A guard that can never be satisfied is not a safeguard.
/// </para>
/// <para>
/// So the operator is told what is winding down instead of being stopped. See <c>NET-025</c>.
/// </para>
/// </summary>
/// <param name="Copies">Volumes on the shelves, across every title.</param>
/// <param name="ActiveReservations">Loans still out or in transit. They remain returnable here.</param>
/// <param name="UnresolvedFines">Fines outstanding or awaiting validation. They remain payable.</param>
public sealed record LibraryObligations(int Copies, int ActiveReservations, int UnresolvedFines)
{
    public static readonly LibraryObligations None = new(0, 0, 0);

    public bool HasAny => Copies > 0 || ActiveReservations > 0 || UnresolvedFines > 0;
}
