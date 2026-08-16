using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Support.Errors;

public static class SupportErrors
{
    public static readonly Error TicketNotFound =
        Error.NotFound("support.ticket_not_found", "That ticket does not exist.");

    /// <summary>BR-SUP-004. Authorization rather than not-found: staff asking would receive it.</summary>
    public static readonly Error NotYours =
        Error.Authorization("support.not_yours", "That ticket is not yours.");

    public static readonly Error OutOfScope =
        Error.Authorization("support.out_of_scope",
            "That ticket belongs to a library you do not administer.");

    public static readonly Error SubjectRequired =
        Error.Validation("support.subject_required", "Say what the ticket is about.");

    public static readonly Error MessageRequired =
        Error.Validation("support.message_required", "A message cannot be empty.");

    /// <summary>
    /// BR-SUP-011. Reopening is a deliberate act; letting a reply do it silently would reopen
    /// tickets nobody meant to and put them back in a queue somebody had finished with.
    /// </summary>
    public static readonly Error TicketIsResolved =
        Error.Conflict("support.ticket_is_resolved",
            "This ticket is resolved. Reopen it to carry on the conversation.");

    public static readonly Error TicketNotResolved =
        Error.Conflict("support.ticket_not_resolved",
            "A ticket can only be rated once it is resolved.");

    public static readonly Error TicketAlreadyResolved =
        Error.Conflict("support.ticket_already_resolved", "That ticket is already resolved.");

    public static readonly Error TicketNotReopenable =
        Error.Conflict("support.ticket_not_reopenable", "Only a resolved ticket can be reopened.");

    public static readonly Error AgentRequired =
        Error.Conflict("support.agent_required", "Assign an agent before moving this into review.");

    public static readonly Error RatingOutOfRange =
        Error.Validation("support.rating_out_of_range", "A rating is between one and five stars.");
}
