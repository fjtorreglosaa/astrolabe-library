using Astrolabe.Domain.Features.Catalog.Enums;
using Astrolabe.Domain.Features.Reservations.Errors;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Shared.Reservations;

/// <summary>
/// Turns a catalogue refusal into the error a reservation returns.
///
/// The mapping lives here rather than in the handler because the same refusal has to read the same
/// way whether it arrives while quoting or while confirming — and a member who is told one thing on
/// the modal and another on the button has been told nothing.
/// </summary>
public static class ReservationAccess
{
    /// <param name="homeLibraryName">
    /// The member's <b>own</b> home library, never the one they asked for. Naming the requested
    /// branch would produce "Basic borrows at Loop only" while refusing them at Loop — a sentence
    /// that is exactly backwards and sends the member to the wrong shelf.
    /// </param>
    /// <param name="copyCityName">The city the copy sits in, which is the one out of reach.</param>
    public static Error ToError(CopyRejection? reason, string? homeLibraryName, string? copyCityName) =>
        reason switch
        {
            CopyRejection.OutOfStock => ReservationErrors.CopyJustTaken,

            CopyRejection.NotInBasicCatalog => Error.Authorization(
                "reservations.not_in_basic_catalog",
                "This title is not in the Basic catalog. Upgrade your plan to borrow it."),

            CopyRejection.HomeLibraryOnly => Error.Authorization(
                "reservations.home_library_only",
                homeLibraryName is null
                    ? "The Basic plan borrows at your home library only."
                    : $"The Basic plan borrows at {homeLibraryName} only."),

            CopyRejection.OutsideCity => Error.Authorization(
                "reservations.outside_city",
                copyCityName is null
                    ? "That copy is outside your city."
                    : $"That copy is in {copyCityName}, outside the libraries your plan reaches."),

            // A verdict with no reason means the copy was reservable, so reaching here at all is a
            // defect rather than a refusal. Failing closed is the only safe answer.
            _ => ReservationErrors.CopyJustTaken
        };
}
