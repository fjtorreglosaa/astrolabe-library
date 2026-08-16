using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Billing.Errors;

public static class BillingErrors
{
    public static readonly Error FineNotFound =
        Error.NotFound("billing.fine_not_found", "That fine does not exist.");

    public static readonly Error FineNotYours =
        Error.Authorization("billing.fine_not_yours", "That fine is not yours.");

    public static readonly Error NothingToPay =
        Error.Validation("billing.nothing_to_pay", "You have no outstanding fines. Nothing to pay.");

    public static readonly Error FineAlreadyPaid =
        Error.Conflict("billing.fine_already_paid", "That fine has already been paid.");

    /// <summary>
    /// BR-BIL-021. Without this the librarian validates a debt the card already cleared, and the
    /// member pays for one book twice.
    /// </summary>
    public static readonly Error FineAwaitingValidation =
        Error.Conflict("billing.fine_awaiting_validation",
            "That fine is waiting to be paid at a library desk. Cancel the code first.");

    /// <summary>
    /// BR-BIL-023, which follows from BR-BIL-005: only the owning library's staff may validate a
    /// code, so a code spanning two libraries could be validated at neither.
    /// </summary>
    public static readonly Error FinesSpanLibraries =
        Error.Validation("billing.fines_span_libraries",
            "One payment code covers fines from one library. Generate a separate code for each.");

    public static readonly Error PaymentMethodNotFound =
        Error.NotFound("billing.payment_method_not_found", "That payment method is not on file.");

    /// <summary>
    /// BR-BIL-006. Deliberately blunt: a caller sending a full number is refused outright rather
    /// than having it silently truncated into storage.
    /// </summary>
    public static readonly Error CardDetailsInvalid =
        Error.Validation("billing.card_details_invalid",
            "Send only the last four digits of the card. A full card number is never accepted.");

    public static readonly Error ExpiryInvalid =
        Error.Validation("billing.expiry_invalid", "An expiry must look like 09/28.");

    public static readonly Error CardholderRequired =
        Error.Validation("billing.cardholder_required", "A cardholder name is required.");

    public static readonly Error DeskPaymentNotFound =
        Error.NotFound("billing.desk_payment_not_found", "That payment code does not exist.");

    public static readonly Error DeskPaymentExpired =
        Error.Conflict("billing.desk_payment_expired",
            "That payment code has expired. The member can generate a new one.");

    public static readonly Error DeskPaymentAlreadyResolved =
        Error.Conflict("billing.desk_payment_already_resolved",
            "That payment code has already been dealt with.");

    public static readonly Error RejectionReasonRequired =
        Error.Validation("billing.rejection_reason_required",
            "Say why the payment is being rejected.");

    public static readonly Error LibraryOutOfScope =
        Error.Authorization("billing.library_out_of_scope",
            "You can only validate payments for your own libraries.");
}
