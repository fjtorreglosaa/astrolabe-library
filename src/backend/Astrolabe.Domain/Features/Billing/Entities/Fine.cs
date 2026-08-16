using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Billing.Enums;
using Astrolabe.Domain.Features.Billing.Errors;
using Astrolabe.Domain.Features.Billing.Events;
using Astrolabe.Domain.Features.Billing.Policies;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Billing.Entities;

/// <summary>
/// What one member owes for one late title. Implements BR-BIL-001 to BR-BIL-003 and BR-BIL-009.
///
/// <para>
/// The amount is <b>frozen at assessment</b>. <c>reservations</c> already recorded the days late at
/// check-in, and pricing from that number once is what makes BR-BIL-003 true: a fine that recomputed
/// itself would keep growing after the book was back on the shelf, and nobody would notice until it
/// appeared on a real bill.
/// </para>
/// </summary>
public sealed class Fine : AggregateRoot
{
    private Fine()
    {
    }

    private Fine(
        Guid id, Guid memberId, Guid reservationId, Guid libraryId,
        string bookTitle, int daysLate, Money amount, DateTimeOffset now) : base(id)
    {
        MemberId = memberId;
        ReservationId = reservationId;
        LibraryId = libraryId;
        BookTitle = bookTitle;
        DaysLate = daysLate;
        Amount = amount;
        Status = FineStatus.Outstanding;
        AssessedAt = now;

        Raise(new FineAssessed(Guid.NewGuid(), now, id, memberId, reservationId, daysLate, amount));
    }

    public Guid MemberId { get; private set; }

    /// <summary>Uniquely indexed, which is what makes BR-BIL-010 hold under a retry.</summary>
    public Guid ReservationId { get; private set; }

    /// <summary>The library owed the money, and the only one whose staff may take it at a desk.</summary>
    public Guid LibraryId { get; private set; }

    /// <summary>
    /// Copied, not referenced. A statement has to be readable years later, and a book removed from
    /// the catalogue must not turn a line of somebody's bill into a blank.
    /// </summary>
    public string BookTitle { get; private set; } = string.Empty;

    public int DaysLate { get; private set; }

    public Money Amount { get; private set; }

    public FineStatus Status { get; private set; }

    public DateTimeOffset AssessedAt { get; private set; }

    public DateTimeOffset? SettledAt { get; private set; }

    /// <summary>The desk code holding this fine, if one is open.</summary>
    public Guid? DeskPaymentId { get; private set; }

    public bool IsOutstanding => Status is FineStatus.Outstanding;

    /// <summary>Owed, whether or not a desk code is holding it. Only paying clears a debt.</summary>
    public bool IsOwed => Status is not FineStatus.Paid;

    /// <summary>
    /// Prices a late return. Returns null when nothing is owed: BR-BIL-009 makes an on-time return
    /// produce no fine at all rather than a fine of zero, which would clutter every statement with
    /// lines saying nothing happened.
    /// </summary>
    public static Fine? Assess(
        Guid memberId, Guid reservationId, Guid libraryId,
        string bookTitle, int daysLate, DateTimeOffset now)
    {
        var amount = FinePolicy.For(daysLate);

        if (amount.IsZero)
        {
            return null;
        }

        return new Fine(
            Guid.NewGuid(), memberId, reservationId, libraryId,
            string.IsNullOrWhiteSpace(bookTitle) ? "Unknown title" : bookTitle.Trim(),
            daysLate, amount, now);
    }

    /// <summary>
    /// Holds the fine against a desk code. BR-BIL-017: this settles nothing — the member has not
    /// paid — but it stops the same debt being cleared by card at the same time.
    /// </summary>
    public Result Hold(Guid deskPaymentId)
    {
        if (Status is FineStatus.Paid)
        {
            return Result.Failure(BillingErrors.FineAlreadyPaid);
        }

        if (Status is FineStatus.AwaitingValidation)
        {
            return Result.Failure(BillingErrors.FineAwaitingValidation);
        }

        Status = FineStatus.AwaitingValidation;
        DeskPaymentId = deskPaymentId;

        return Result.Success();
    }

    /// <summary>BR-BIL-019 and BR-BIL-020: a rejected or expired code leaves the debt owed as before.</summary>
    public void Release()
    {
        if (Status is FineStatus.AwaitingValidation)
        {
            Status = FineStatus.Outstanding;
            DeskPaymentId = null;
        }
    }

    /// <summary>
    /// Settles the fine. Idempotent: a repeated payment must not write a second ledger entry, so the
    /// caller reads the result to know whether anything moved.
    /// </summary>
    public Result<bool> Settle(DateTimeOffset now)
    {
        if (Status is FineStatus.Paid)
        {
            return Result.Success(false);
        }

        Status = FineStatus.Paid;
        SettledAt = now;

        Raise(new FinePaid(Guid.NewGuid(), now, Id, MemberId, Amount));

        return Result.Success(true);
    }
}
