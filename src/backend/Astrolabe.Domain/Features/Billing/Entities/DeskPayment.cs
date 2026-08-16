using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Billing.Enums;
using Astrolabe.Domain.Features.Billing.Errors;
using Astrolabe.Domain.Features.Billing.Events;
using Astrolabe.Domain.Features.Billing.ValueObjects;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Billing.Entities;

/// <summary>
/// A code the member takes to a library counter. Implements BR-BIL-004, BR-BIL-005 and BR-BIL-017 to
/// BR-BIL-020.
///
/// <para>
/// Issuing one <b>settles nothing</b>. Nobody has paid when a code is printed, and marking the debt
/// cleared at that moment would let any member wipe their account by generating a code and never
/// going in. The librarian taking the money is the event that matters, and validation is how they
/// record it.
/// </para>
/// </summary>
public sealed class DeskPayment : AggregateRoot
{
    /// <summary>BR-BIL-004. The prototype tells the member: "the code expires in 72 hours".</summary>
    public static readonly TimeSpan Validity = TimeSpan.FromHours(72);

    private readonly List<Guid> _fineIds = [];

    private DeskPayment()
    {
    }

    private DeskPayment(
        Guid id, PaymentCode code, Guid memberId, Guid libraryId,
        Money amount, IEnumerable<Guid> fineIds, DateTimeOffset now) : base(id)
    {
        Code = code;
        MemberId = memberId;
        LibraryId = libraryId;
        Amount = amount;
        Status = DeskPaymentStatus.Pending;
        IssuedAt = now;
        ExpiresAt = now.Add(Validity);
        _fineIds.AddRange(fineIds);

        Raise(new DeskPaymentIssued(Guid.NewGuid(), now, id, memberId, libraryId, amount, ExpiresAt));
    }

    public PaymentCode Code { get; private set; } = null!;

    public Guid MemberId { get; private set; }

    /// <summary>The library that may take the money. BR-BIL-005 turns on this.</summary>
    public Guid LibraryId { get; private set; }

    public Money Amount { get; private set; }

    public DeskPaymentStatus Status { get; private set; }

    public DateTimeOffset IssuedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? ResolvedAt { get; private set; }

    public string? RejectionReason { get; private set; }

    public IReadOnlyList<Guid> FineIds => _fineIds;

    /// <summary>
    /// Derived rather than swept by a job — the same reasoning as overdue in <c>reservations</c>. A
    /// job that failed would leave stale codes looking valid at a counter, which is money.
    /// </summary>
    public bool IsExpiredAt(DateTimeOffset now) =>
        Status is DeskPaymentStatus.Pending && now > ExpiresAt;

    public bool IsPendingAt(DateTimeOffset now) =>
        Status is DeskPaymentStatus.Pending && !IsExpiredAt(now);

    public static DeskPayment Issue(
        Guid memberId, Guid libraryId, Money amount,
        IEnumerable<Guid> fineIds, DateTimeOffset now)
    {
        var id = Guid.NewGuid();

        return new DeskPayment(id, PaymentCode.Generate(id), memberId, libraryId, amount, fineIds, now);
    }

    /// <summary>
    /// The librarian took the money. This is the only thing that settles the fines the code covers.
    /// </summary>
    public Result Validate(DateTimeOffset now)
    {
        var guard = EnsureActionable(now);

        if (guard.IsFailure)
        {
            return guard;
        }

        Status = DeskPaymentStatus.Validated;
        ResolvedAt = now;

        Raise(new DeskPaymentValidated(Guid.NewGuid(), now, Id, MemberId, LibraryId, Amount));

        return Result.Success();
    }

    /// <summary>
    /// The member never came, or the money was not taken. BR-BIL-019 requires a stated reason,
    /// because a rejection puts a debt back on somebody's account and they are entitled to know why.
    /// </summary>
    public Result Reject(string? reason, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure(BillingErrors.RejectionReasonRequired);
        }

        var guard = EnsureActionable(now);

        if (guard.IsFailure)
        {
            return guard;
        }

        Status = DeskPaymentStatus.Rejected;
        RejectionReason = reason.Trim();
        ResolvedAt = now;

        Raise(new DeskPaymentRejected(Guid.NewGuid(), now, Id, MemberId, RejectionReason));

        return Result.Success();
    }

    /// <summary>Records that a stale code was found. Its fines go back to outstanding.</summary>
    public void MarkExpired(DateTimeOffset now)
    {
        if (Status is DeskPaymentStatus.Pending)
        {
            Status = DeskPaymentStatus.Expired;
            ResolvedAt = now;
        }
    }

    /// <summary>
    /// BR-BIL-020: an expired code can be neither validated nor rejected. Checked before either, so
    /// a librarian cannot take money against a code that ran out while the member queued.
    /// </summary>
    private Result EnsureActionable(DateTimeOffset now)
    {
        if (Status is not DeskPaymentStatus.Pending)
        {
            return Result.Failure(BillingErrors.DeskPaymentAlreadyResolved);
        }

        if (IsExpiredAt(now))
        {
            return Result.Failure(BillingErrors.DeskPaymentExpired);
        }

        return Result.Success();
    }
}
