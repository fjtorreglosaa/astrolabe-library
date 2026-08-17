namespace Astrolabe.Application.Contracts.Realtime;

/// <summary>
/// Every name a <see cref="RealtimeEvent"/> may carry.
/// </summary>
/// <remarks>
/// <para>
/// Named after <b>what happened in the business</b>, not after the screen that reacts. "A fine was
/// paid" stays true when the fines screen is redesigned, split in two, or shown to a librarian as
/// well; "refresh the fines table" does not. The mapping from an event to the queries it invalidates
/// is a client concern and lives in the client.
/// </para>
/// <para>
/// Constants rather than an enum. These strings cross the wire to TypeScript, and an enum would be
/// serialized as a number that silently renumbers the day somebody inserts a member in the middle.
/// </para>
/// </remarks>
public static class RealtimeEventNames
{
    // ---------- Reservations ----------

    /// <summary>A book was reserved. The copy count moved and a loan appeared.</summary>
    public const string ReservationConfirmed = "reservation.confirmed";

    /// <summary>A return was started and a code was issued. The member has something to show a desk.</summary>
    public const string ReturnStarted = "reservation.return-started";

    /// <summary>A return was accepted at a desk. The loan is closed and the copy is back.</summary>
    public const string ReservationReturned = "reservation.returned";

    // ---------- Billing ----------

    /// <summary>A fine was raised against a member.</summary>
    public const string FineAssessed = "billing.fine-assessed";

    /// <summary>A fine was settled by card.</summary>
    public const string FinePaid = "billing.fine-paid";

    /// <summary>A desk payment code was produced. Nothing is charged yet.</summary>
    public const string DeskPaymentIssued = "billing.desk-payment-issued";

    /// <summary>A librarian confirmed the money was taken.</summary>
    public const string DeskPaymentValidated = "billing.desk-payment-validated";

    /// <summary>A librarian refused a code. The fines stay owed.</summary>
    public const string DeskPaymentRejected = "billing.desk-payment-rejected";

    // ---------- Store ----------

    /// <summary>A purchase went through. Points and the statement moved with it.</summary>
    public const string OrderPlaced = "store.order-placed";

    // ---------- Support ----------

    /// <summary>A ticket gained a message.</summary>
    public const string TicketAnswered = "support.ticket-answered";

    // ---------- Notifications ----------

    /// <summary>
    /// A notification was written for this member. Its own name rather than a rider on the events
    /// above, because a member can mute a family: the thing happened, and they still hear nothing.
    /// </summary>
    public const string NotificationRaised = "notifications.raised";

    // ---------- Identity ----------

    /// <summary>
    /// This member's access ended somewhere — a session was revoked, or the account was blocked.
    /// The only event a client acts on rather than merely refetching.
    /// </summary>
    public const string AccessRevoked = "identity.access-revoked";
}
