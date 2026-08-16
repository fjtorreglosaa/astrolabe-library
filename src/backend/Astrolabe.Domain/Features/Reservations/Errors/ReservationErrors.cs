using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Reservations.Errors;

public static class ReservationErrors
{
    public static readonly Error NotFound =
        Error.NotFound("reservations.not_found", "That reservation does not exist.");

    /// <summary>
    /// BR-RSV-006 under concurrency. Worded as "just taken" rather than "out of stock": the member
    /// saw it available a second ago, and telling them it never existed would read as a bug.
    /// </summary>
    public static readonly Error CopyJustTaken =
        Error.Conflict("reservations.copy_just_taken",
            "Someone reserved the last copy a moment ago. Try another library.");

    public static readonly Error NoCopyAtLibrary =
        Error.NotFound("reservations.no_copy_at_library",
            "That library does not hold a copy of this book.");

    public static readonly Error AlreadyReserved =
        Error.Conflict("reservations.already_reserved",
            "You already have this copy reserved.");

    public static readonly Error NotYours =
        Error.Authorization("reservations.not_yours", "That reservation is not yours.");

    public static readonly Error InvalidHandoverCode =
        Error.Validation("reservations.invalid_handover_code",
            "That code is not valid. Ask them to read it again.");

    public static readonly Error AlreadyInTransit =
        Error.Conflict("reservations.already_in_transit",
            "This copy is already on its way back to the library.");

    public static readonly Error NotReturnable =
        Error.Conflict("reservations.not_returnable",
            "This reservation is no longer open.");

    public static readonly Error LibraryOutOfScope =
        Error.Authorization("reservations.library_out_of_scope",
            "You can only check in copies belonging to your libraries.");

    public static readonly Error NotHandedOver =
        Error.Conflict("reservations.not_handed_over",
            "The member has not handed this copy over yet.");
}
