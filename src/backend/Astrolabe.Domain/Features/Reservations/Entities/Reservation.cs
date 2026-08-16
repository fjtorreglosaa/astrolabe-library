using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Reservations.Enums;
using Astrolabe.Domain.Features.Reservations.Errors;
using Astrolabe.Domain.Features.Reservations.Events;
using Astrolabe.Domain.Features.Reservations.ValueObjects;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Reservations.Entities;

/// <summary>
/// One member holding one physical copy from one library, until it is back on the shelf.
/// Implements BR-RSV-001 to BR-RSV-020.
///
/// <para>
/// The return is two acts by two people: the member hands the copy over, and the library receives
/// it. The state between them is the physical truth — the copy is somewhere on a van — and
/// collapsing it into one step would let stock come back before the book does.
/// </para>
/// </summary>
public sealed class Reservation : AggregateRoot
{
    /// <summary>BR-RSV-003. The prototype's `DELIVERY_FEE`, in cents like every other amount.</summary>
    public static readonly Money HomeDeliveryFee = Money.FromUnits(3, 99);

    private Reservation()
    {
    }

    private Reservation(
        Guid id, Guid memberId, Guid bookId, Guid bookCopyId, Guid libraryId,
        LoanPeriod period, DeliveryMethod delivery, Money deliveryFee,
        string? idempotencyKey, DateTimeOffset now) : base(id)
    {
        MemberId = memberId;
        BookId = bookId;
        BookCopyId = bookCopyId;
        LibraryId = libraryId;
        Period = period;
        Delivery = delivery;
        DeliveryFee = deliveryFee;
        IdempotencyKey = idempotencyKey;
        Status = ReservationStatus.Reserved;
        ConfirmedAt = now;

        Raise(new ReservationConfirmed(
            Guid.NewGuid(), now, id, memberId, bookId, bookCopyId, libraryId, period.DueOn));
    }

    public Guid MemberId { get; private set; }

    public Guid BookId { get; private set; }

    /// <summary>The exact holding the copy came from. BR-RSV-002 makes this one library's shelf.</summary>
    public Guid BookCopyId { get; private set; }

    public Guid LibraryId { get; private set; }

    public LoanPeriod Period { get; private set; } = null!;

    public DeliveryMethod Delivery { get; private set; }

    public Money DeliveryFee { get; private set; }

    public ReservationStatus Status { get; private set; }

    public DateTimeOffset ConfirmedAt { get; private set; }

    public ReturnMethod? ReturnMethod { get; private set; }

    public DateTimeOffset? HandedOverAt { get; private set; }

    public DateTimeOffset? CheckedInAt { get; private set; }

    /// <summary>
    /// Days late at the moment staff received the copy. Frozen then, because the member stops being
    /// responsible when the library has it — not when someone gets round to pricing the fine.
    /// </summary>
    public int DaysLateAtCheckIn { get; private set; }

    /// <summary>Deduplicates a retried confirmation. See BR-RSV-008.</summary>
    public string? IdempotencyKey { get; private set; }

    public bool IsActive => Status is ReservationStatus.Reserved or ReservationStatus.InTransit;

    /// <summary>The code the courier or librarian reads out. Derived, never stored.</summary>
    public HandoverCode HandoverCode => HandoverCode.For(Id);

    /// <summary>
    /// BR-RSV-010. Derived from the clock rather than stored, so it can never disagree with the
    /// calendar and no job has to keep it true.
    /// </summary>
    public bool IsOverdueAt(DateTimeOffset now) => IsActive && Period.IsOverdueAt(now);

    public int DaysLateAt(DateTimeOffset now) =>
        Status is ReservationStatus.Returned ? DaysLateAtCheckIn : Period.DaysLateAt(now);

    // ---------- Confirming ----------

    /// <summary>
    /// Takes a copy. The caller has already asked <c>catalog</c> whether the member may have it and
    /// has already decremented the copy: this records who took it and until when.
    /// </summary>
    public static Reservation Confirm(
        Guid memberId,
        Guid bookId,
        Guid bookCopyId,
        Guid libraryId,
        DeliveryMethod delivery,
        string? idempotencyKey,
        DateTimeOffset now) =>
        new(Guid.NewGuid(), memberId, bookId, bookCopyId, libraryId,
            LoanPeriod.StartingAt(now), delivery, FeeFor(delivery),
            string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey.Trim(), now);

    /// <summary>BR-RSV-003. Quotable without a reservation, so the modal can price before committing.</summary>
    public static Money FeeFor(DeliveryMethod delivery) =>
        delivery is DeliveryMethod.HomeDelivery ? HomeDeliveryFee : Money.Zero;

    // ---------- Returning ----------

    /// <summary>
    /// The member's half of the return. Implements BR-RSV-013 to BR-RSV-015.
    ///
    /// The code proves a physical handover happened. It does not make the copy available again —
    /// only the library receiving it does that.
    /// </summary>
    public Result BeginReturn(Guid memberId, ReturnMethod method, string? typedCode, DateTimeOffset now)
    {
        if (MemberId != memberId)
        {
            return Result.Failure(ReservationErrors.NotYours);
        }

        if (Status is ReservationStatus.InTransit)
        {
            return Result.Failure(ReservationErrors.AlreadyInTransit);
        }

        if (Status is not ReservationStatus.Reserved)
        {
            return Result.Failure(ReservationErrors.NotReturnable);
        }

        // BR-RSV-014: a wrong code changes nothing at all. Checked before any state moves, so a
        // failed attempt cannot leave the reservation half-way into a return.
        if (!HandoverCode.Matches(typedCode))
        {
            return Result.Failure(ReservationErrors.InvalidHandoverCode);
        }

        Status = ReservationStatus.InTransit;
        ReturnMethod = method;
        HandedOverAt = now;

        Raise(new ReturnStarted(Guid.NewGuid(), now, Id, MemberId, BookCopyId, method));

        return Result.Success();
    }

    /// <summary>
    /// The library's half. Implements BR-RSV-016 to BR-RSV-020.
    ///
    /// <para>
    /// Idempotent by BR-RSV-019: two members of staff scanning the same parcel must not restore two
    /// volumes to a shelf that only lost one. The caller reads <see cref="RestoresStock"/> to know
    /// whether to put the copy back.
    /// </para>
    /// </summary>
    public Result<bool> CheckIn(DateTimeOffset now)
    {
        if (Status is ReservationStatus.Returned)
        {
            return Result.Success(false);
        }

        if (Status is not (ReservationStatus.Reserved or ReservationStatus.InTransit))
        {
            return Result.Failure<bool>(ReservationErrors.NotReturnable);
        }

        // Frozen at the moment the library takes responsibility, not at the moment billing looks.
        DaysLateAtCheckIn = Period.DaysLateAt(now);
        Status = ReservationStatus.Returned;
        CheckedInAt = now;

        // BR-RSV-020: the number travels, the price does not. billing owns the rate and the cap.
        Raise(new ReservationReturned(
            Guid.NewGuid(), now, Id, MemberId, BookId, BookCopyId, LibraryId, DaysLateAtCheckIn));

        return Result.Success(true);
    }

    /// <summary>Whether check-in put a volume back. False on a repeated check-in.</summary>
    public bool RestoresStock => Status is ReservationStatus.Returned;

    /// <summary>A confirmation that failed after taking stock. Not reachable from the interface.</summary>
    public void Cancel() => Status = ReservationStatus.Cancelled;
}
