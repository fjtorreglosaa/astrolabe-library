using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Billing.Enums;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Billing.Entities;

/// <summary>
/// One movement of money. Implements BR-BIL-011 to BR-BIL-013.
///
/// <para>
/// <b>Append-only.</b> There is no method that changes anything on this type after construction, and
/// the repository contract offers no update or delete. A mistake is corrected by a compensating
/// entry, which leaves both movements visible — a ledger that can be edited is not a ledger, it is a
/// number with a history nobody can trust.
/// </para>
/// <para>
/// A balance is the sum of these. It is never stored: a stored balance is a second source of truth
/// for something the entries already say, and the day the two disagree there is no way to tell which
/// is right.
/// </para>
/// </summary>
public sealed class LedgerEntry : Entity
{
    private LedgerEntry()
    {
    }

    private LedgerEntry(
        Guid id, Guid memberId, LedgerEntryKind kind, Money amount,
        string description, Guid? fineId, Guid? reservationId, DateTimeOffset now) : base(id)
    {
        MemberId = memberId;
        Kind = kind;
        Amount = amount;
        Description = description;
        FineId = fineId;
        ReservationId = reservationId;
        OccurredAt = now;
    }

    public Guid MemberId { get; private set; }

    public LedgerEntryKind Kind { get; private set; }

    /// <summary>Signed. A charge is negative, so a balance is a plain sum rather than a case analysis.</summary>
    public Money Amount { get; private set; }

    /// <summary>What it was for, in the member's own terms. BR-BIL-013.</summary>
    public string Description { get; private set; } = string.Empty;

    public Guid? FineId { get; private set; }

    public Guid? ReservationId { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>Money owed. Recorded negative so the balance needs no interpretation.</summary>
    public static LedgerEntry Charge(
        Guid memberId, Money amount, string description,
        Guid? fineId, Guid? reservationId, DateTimeOffset now) =>
        new(Guid.NewGuid(), memberId, LedgerEntryKind.Charge,
            Money.FromCents(-Math.Abs(amount.Cents)), description, fineId, reservationId, now);

    /// <summary>Money settled. Positive, so it offsets the charge it answers.</summary>
    public static LedgerEntry Payment(
        Guid memberId, Money amount, string description, Guid? fineId, DateTimeOffset now) =>
        new(Guid.NewGuid(), memberId, LedgerEntryKind.Payment,
            Money.FromCents(Math.Abs(amount.Cents)), description, fineId, null, now);

    /// <summary>
    /// Money returned, or a correction. The only way to undo a mistake, precisely because it leaves
    /// the original entry standing.
    /// </summary>
    public static LedgerEntry Credit(
        Guid memberId, Money amount, string description, DateTimeOffset now) =>
        new(Guid.NewGuid(), memberId, LedgerEntryKind.Credit,
            Money.FromCents(Math.Abs(amount.Cents)), description, null, null, now);
}
