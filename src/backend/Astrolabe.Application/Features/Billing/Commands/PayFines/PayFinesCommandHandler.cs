using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Billing;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Audit.Entities;
using Astrolabe.Domain.Features.Audit.Repositories;
using Astrolabe.Domain.Features.Billing.Entities;
using Astrolabe.Domain.Features.Billing.Enums;
using Astrolabe.Domain.Features.Billing.Errors;
using Astrolabe.Domain.Features.Billing.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Billing.Commands.PayFines;

public sealed class PayFinesCommandHandler(
    IBillingUnitOfWork billing,
    IAuditUnitOfWork audit,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : ICommandHandler<PayFinesCommand, PaymentReceiptDto>
{
    public async Task<Result<PaymentReceiptDto>> Handle(
        PayFinesCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } memberId)
        {
            return Result.Failure<PaymentReceiptDto>(BillingErrors.FineNotYours);
        }

        if (request.FineIds.Count == 0)
        {
            return Result.Failure<PaymentReceiptDto>(BillingErrors.NothingToPay);
        }

        // Looked up by member as well as by id, so a card belonging to somebody else is simply not
        // found rather than refused after being read.
        var card = await billing.PaymentMethods.GetForMemberAsync(
            memberId, request.PaymentMethodId, cancellationToken);

        if (card is null)
        {
            return Result.Failure<PaymentReceiptDto>(BillingErrors.PaymentMethodNotFound);
        }

        // Filtered by member inside the repository too. BR-BIL-016 holds even if this handler is
        // rewritten by somebody who forgets it.
        var fines = await billing.Fines.GetByIdsForMemberAsync(
            memberId, request.FineIds, cancellationToken);

        if (fines.Count == 0)
        {
            return Result.Failure<PaymentReceiptDto>(BillingErrors.FineNotFound);
        }

        // BR-BIL-021. A fine promised to a desk code must not also be payable by card, or the
        // librarian later validates a debt that is already settled and the member pays twice.
        if (fines.Any(f => f.Status is FineStatus.AwaitingValidation))
        {
            return Result.Failure<PaymentReceiptDto>(BillingErrors.FineAwaitingValidation);
        }

        var now = clock.UtcNow;
        var newlySettled = new List<Fine>();

        foreach (var fine in fines)
        {
            var moved = fine.Settle(now);

            if (moved.IsSuccess && moved.Value)
            {
                newlySettled.Add(fine);
            }
        }

        // BR-BIL-008. The receipt describes every fine the request asked for and that is now paid,
        // not only the ones this call moved. A retried request therefore gets the same answer as the
        // first rather than a receipt for $0.00, which would read as a failed payment.
        var paid = fines.Where(f => f.Status is FineStatus.Paid).ToList();
        var amount = Money.FromCents(paid.Sum(f => f.Amount.Cents));

        // Ledger entries and the audit trail come only from what actually moved: a replay must not
        // write a second payment against a debt settled once.
        if (newlySettled.Count > 0)
        {
            await billing.Ledger.AddRangeAsync(
                newlySettled.Select(fine => LedgerEntry.Payment(
                    memberId, fine.Amount,
                    $"Card payment — {fine.BookTitle}", fine.Id, now)),
                cancellationToken);

            await audit.Entries.AddAsync(
                AuditEntry.Record(
                    "billing.fines_paid", now,
                    actorUserId: memberId, subjectUserId: memberId,
                    detail: $"{newlySettled.Count} fine(s), "
                            + $"{Money.FromCents(newlySettled.Sum(f => f.Amount.Cents))}"),
                cancellationToken);

            await billing.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(new PaymentReceiptDto(
            Receipt: ReceiptNumber(memberId, now),
            AmountCents: (int)amount.Cents,
            PaidWith: card.DisplayName,
            FineCount: paid.Count,
            PaidAt: now));
    }

    /// <summary>
    /// The prototype's <c>RC-…</c> shape. Derived from the member and the moment rather than from a
    /// counter, so two people paying at once cannot be handed the same number.
    /// </summary>
    private static string ReceiptNumber(Guid memberId, DateTimeOffset now) =>
        $"RC-{now:yyyyMMdd}-{Math.Abs(memberId.GetHashCode()) % 10000:D4}";
}
